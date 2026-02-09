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
            CreatedAt = t.CreatedAt
        });
    }

    public async Task<TransactionResponse?> GetByIdAsync(int id, int userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
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
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);

        if (user == null || category == null)
            throw new InvalidOperationException("使用者或分類不存在");

        var transaction = new Transaction
        {
            UserId = userId,
            Amount = request.Amount,
            Type = request.Type,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Date = request.Date,
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
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionResponse?> UpdateAsync(int id, UpdateTransactionRequest request, int userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
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

        transaction.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Transactions.Update(transaction);
        await _unitOfWork.SaveChangesAsync();

        // 重新載入以取得最新的 Category
        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync();

        return new TransactionResponse
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category.Name,
            Description = transaction.Description,
            Date = transaction.Date,
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
