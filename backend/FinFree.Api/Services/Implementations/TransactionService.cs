using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public TransactionService(IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IEnumerable<TransactionResponse>> GetAllAsync(int userId)
    {
        var transactions = await _unitOfWork.Transactions.Query()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        return transactions.Select(MapToResponse);
    }

    public async Task<TransactionResponse?> GetByIdAsync(int id, int userId)
    {
        var transaction = await _unitOfWork.Transactions.Query()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        return transaction == null ? null : MapToResponse(transaction);
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);

        if (user == null || category == null)
            throw new InvalidOperationException("使用者或分類不存在");

        if (category.UserId != null && category.UserId != userId)
            throw new InvalidOperationException("無權使用此分類");

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            throw new InvalidOperationException("帳戶不存在");

        if (account.UserId != userId)
            throw new InvalidOperationException("無權使用此帳戶");

        var transaction = new Transaction
        {
            UserId = userId,
            Amount = request.Amount,
            Type = request.Type,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Date = request.Date,
            AccountId = request.AccountId,
            User = user,
            Category = category
        };

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();
        InvalidateStatisticsCache(userId);

        return new TransactionResponse
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CategoryId = transaction.CategoryId,
            CategoryName = category.Name,
            Description = transaction.Description,
            Date = transaction.Date,
            AccountId = transaction.AccountId,
            AccountName = account.Name,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionResponse?> UpdateAsync(int id, UpdateTransactionRequest request, int userId)
    {
        var transaction = await _unitOfWork.Transactions.Query()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return null;

        if (request.Amount.HasValue)
            transaction.Amount = request.Amount.Value;

        if (request.Type.HasValue)
            transaction.Type = request.Type.Value;

        if (request.CategoryId.HasValue)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId.Value);
            if (category != null)
                transaction.CategoryId = request.CategoryId.Value;
        }

        if (request.Description != null)
            transaction.Description = request.Description;

        if (request.Date.HasValue)
            transaction.Date = request.Date.Value;

        if (request.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId.Value);
            if (account == null || account.UserId != userId)
                throw new InvalidOperationException("無權使用此帳戶");
            transaction.AccountId = request.AccountId.Value;
        }

        transaction.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Transactions.Update(transaction);
        await _unitOfWork.SaveChangesAsync();
        InvalidateStatisticsCache(userId);

        // 重新載入以取得最新的 Category 和 Account
        var updated = await _unitOfWork.Transactions.Query()
            .Where(t => t.Id == transaction.Id)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync();

        return MapToResponse(updated ?? transaction);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var transaction = await _unitOfWork.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return false;

        _unitOfWork.Transactions.Delete(transaction);
        await _unitOfWork.SaveChangesAsync();
        InvalidateStatisticsCache(userId);

        return true;
    }

    public async Task<byte[]> ExportCsvAsync(int userId)
    {
        var transactions = await _unitOfWork.Transactions.Query()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(true));

        await writer.WriteLineAsync("日期,類型,分類,金額,備註");

        foreach (var t in transactions)
        {
            var type = t.Type == TransactionType.Income ? "收入" : "支出";
            var description = EscapeCsv(t.Description ?? "");
            await writer.WriteLineAsync($"{t.Date:yyyy-MM-dd},{type},{EscapeCsv(t.Category.Name)},{t.Amount},{description}");
        }

        await writer.FlushAsync();
        return stream.ToArray();
    }

    public async Task<(int imported, int failed, List<string> errors)> ImportCsvAsync(Stream csvStream, int accountId, int userId)
    {
        var imported = 0;
        var failed = 0;
        var errors = new List<string>();

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        if (account is null)
            throw new InvalidOperationException("帳戶不存在");

        if (account.UserId != userId)
            throw new InvalidOperationException("無權使用此帳戶");

        using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null) return (0, 0, errors);

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var fields = ParseCsvLine(line);
                if (fields.Length < 4)
                {
                    errors.Add($"第 {lineNumber} 行：欄位數不足");
                    failed++;
                    continue;
                }

                if (!DateTime.TryParse(fields[0].Trim(), out var date))
                {
                    errors.Add($"第 {lineNumber} 行：日期格式錯誤「{fields[0]}」");
                    failed++;
                    continue;
                }

                var typeStr = fields[1].Trim();
                TransactionType type;
                if (typeStr == "收入") type = TransactionType.Income;
                else if (typeStr == "支出") type = TransactionType.Expense;
                else
                {
                    errors.Add($"第 {lineNumber} 行：類型必須為「收入」或「支出」，實際為「{typeStr}」");
                    failed++;
                    continue;
                }

                var categoryName = fields[2].Trim();
                if (string.IsNullOrEmpty(categoryName))
                {
                    errors.Add($"第 {lineNumber} 行：分類不可為空");
                    failed++;
                    continue;
                }

                if (!decimal.TryParse(fields[3].Trim(), out var amount) || amount <= 0)
                {
                    errors.Add($"第 {lineNumber} 行：金額格式錯誤「{fields[3]}」");
                    failed++;
                    continue;
                }

                var description = fields.Length > 4 ? fields[4].Trim() : null;

                var category = await _unitOfWork.Categories.Query()
                    .FirstOrDefaultAsync(c => c.Name == categoryName && c.Type == type && (c.UserId == userId || c.UserId == null));

                if (category == null)
                {
                    category = new Category { Name = categoryName, Type = type, UserId = userId };
                    await _unitOfWork.Categories.AddAsync(category);
                    await _unitOfWork.SaveChangesAsync();
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user is null) throw new InvalidOperationException("使用者不存在");

                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = amount,
                    Type = type,
                    CategoryId = category.Id,
                    Description = string.IsNullOrEmpty(description) ? null : description,
                    Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                    AccountId = accountId,
                    User = user,
                    Category = category,
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"第 {lineNumber} 行：{ex.Message}");
                failed++;
            }
        }

        if (imported > 0)
            await _unitOfWork.SaveChangesAsync();

        return (imported, failed, errors);
    }

    public async Task<MonthlyReportResponse> GetMonthlyReportAsync(int year, int month, int userId)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var transactions = await _unitOfWork.Transactions.Query()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var expenseByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category.Name)
            .Select(g => new { Name = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .Select(x => new CategoryBreakdown
            {
                CategoryName = x.Name,
                Amount = x.Amount,
                Percentage = totalExpense > 0 ? Math.Round(x.Amount / totalExpense * 100, 1) : 0,
            })
            .ToList();

        var incomeByCategory = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.Category.Name)
            .Select(g => new { Name = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .Select(x => new CategoryBreakdown
            {
                CategoryName = x.Name,
                Amount = x.Amount,
                Percentage = totalIncome > 0 ? Math.Round(x.Amount / totalIncome * 100, 1) : 0,
            })
            .ToList();

        var dailyExpenses = transactions
            .GroupBy(t => t.Date.ToString("yyyy-MM-dd"))
            .Select(g => new DailyExpense
            {
                Date = g.Key,
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new MonthlyReportResponse
        {
            Year = year,
            Month = month,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            TransactionCount = transactions.Count,
            ExpenseByCategory = expenseByCategory,
            IncomeByCategory = incomeByCategory,
            DailyExpenses = dailyExpenses,
        };
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        // 無日期篩選時快取結果 2 分鐘（Dashboard 使用情境）
        if (!startDate.HasValue && !endDate.HasValue)
        {
            var cacheKey = $"stats:{userId}";
            if (_cache.TryGetValue(cacheKey, out StatisticsResponse? cached) && cached != null)
                return cached;

            var result = await ComputeStatisticsAsync(userId, null, null);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(2));
            return result;
        }

        return await ComputeStatisticsAsync(userId, startDate, endDate);
    }

    private async Task<StatisticsResponse> ComputeStatisticsAsync(int userId, DateTime? startDate, DateTime? endDate)
    {
        var query = _unitOfWork.Transactions.Query()
            .Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date < endDate.Value);

        var transactionList = await query.ToListAsync();

        var totalIncome = transactionList
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactionList
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        return new StatisticsResponse
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            TransactionCount = transactionList.Count
        };
    }

    public void InvalidateStatisticsCache(int userId)
    {
        _cache.Remove($"stats:{userId}");
    }

    private static TransactionResponse MapToResponse(Transaction t) => new()
    {
        Id = t.Id,
        Amount = t.Amount,
        Type = t.Type,
        CategoryId = t.CategoryId,
        CategoryName = t.Category.Name,
        Description = t.Description,
        Date = t.Date,
        AccountId = t.AccountId,
        AccountName = t.Account?.Name,
        CreatedAt = t.CreatedAt
    };

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                i++;
                var sb = new System.Text.StringBuilder();
                while (i < line.Length)
                {
                    if (line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i += 2; }
                    else if (line[i] == '"') { i++; break; }
                    else { sb.Append(line[i]); i++; }
                }
                fields.Add(sb.ToString());
                if (i < line.Length && line[i] == ',') i++;
            }
            else
            {
                var end = line.IndexOf(',', i);
                if (end == -1) { fields.Add(line[i..]); break; }
                fields.Add(line[i..end]);
                i = end + 1;
            }
        }
        return [.. fields];
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
