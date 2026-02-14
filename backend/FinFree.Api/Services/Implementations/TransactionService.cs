using Microsoft.EntityFrameworkCore;
using FinFree.Api.Data;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public TransactionService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<IEnumerable<TransactionResponse>> GetAllAsync(int userId)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        return transactions.Select(t => new TransactionResponse
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
        });
    }

    public async Task<TransactionResponse?> GetByIdAsync(int id, int userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return null;

        return new TransactionResponse
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category.Name,
            Description = transaction.Description,
            Date = transaction.Date,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account?.Name,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);

        if (user == null || category == null)
            throw new InvalidOperationException("使用者或分類不存在");

        // 驗證帳戶
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null)
            throw new InvalidOperationException("帳戶不存在");

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
        var transaction = await _context.Transactions
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
            transaction.AccountId = request.AccountId.Value;

        transaction.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Transactions.Update(transaction);
        await _unitOfWork.SaveChangesAsync();

        // 重新載入以取得最新的 Category 和 Account
        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync();
        await _context.Entry(transaction).Reference(t => t.Account).LoadAsync();

        return new TransactionResponse
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category.Name,
            Description = transaction.Description,
            Date = transaction.Date,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account?.Name,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var transaction = await _unitOfWork.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return false;

        _unitOfWork.Transactions.Delete(transaction);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<byte[]> ExportCsvAsync(int userId)
    {
        var transactions = await _context.Transactions
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
        if (account == null)
            throw new InvalidOperationException("帳戶不存在");

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

                // 尋找或建立分類（優先使用者自訂，其次系統分類）
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName && c.Type == type && (c.UserId == userId || c.UserId == null));

                if (category == null)
                {
                    category = new Category { Name = categoryName, Type = type, UserId = userId };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) throw new InvalidOperationException("使用者不存在");

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
        return fields.ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(int userId)
    {
        var transactions = await _unitOfWork.Transactions
            .FindAsync(t => t.UserId == userId);

        var transactionList = transactions.ToList();

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
}
