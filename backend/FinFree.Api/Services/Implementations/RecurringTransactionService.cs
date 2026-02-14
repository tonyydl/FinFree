using Microsoft.EntityFrameworkCore;
using FinFree.Api.Data;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public RecurringTransactionService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<IEnumerable<RecurringTransactionResponse>> GetAllAsync(int userId)
    {
        var items = await _context.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.Account)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return items.Select(MapToResponse);
    }

    public async Task<RecurringTransactionResponse?> GetByIdAsync(int id, int userId)
    {
        var item = await _context.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        return item == null ? null : MapToResponse(item);
    }

    public async Task<RecurringTransactionResponse> CreateAsync(CreateRecurringTransactionRequest request, int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);

        if (user == null || category == null)
            throw new InvalidOperationException("使用者或分類不存在");

        if (category.UserId != null && category.UserId != userId)
            throw new InvalidOperationException("無權使用此分類");

        // 驗證帳戶
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null)
            throw new InvalidOperationException("帳戶不存在");

        if (account.UserId != userId)
            throw new InvalidOperationException("無權使用此帳戶");

        var item = new RecurringTransaction
        {
            UserId = userId,
            Amount = request.Amount,
            Type = request.Type,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            NextOccurrenceDate = request.StartDate,
            EndDate = request.EndDate,
            AccountId = request.AccountId,
            IsActive = true,
        };

        await _unitOfWork.RecurringTransactions.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        // Reload category and account for response
        await _context.Entry(item).Reference(r => r.Category).LoadAsync();
        await _context.Entry(item).Reference(r => r.Account).LoadAsync();

        return MapToResponse(item);
    }

    public async Task<RecurringTransactionResponse?> UpdateAsync(int id, UpdateRecurringTransactionRequest request, int userId)
    {
        var item = await _context.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (item == null)
            return null;

        if (request.Amount.HasValue)
            item.Amount = request.Amount.Value;

        if (request.Description != null)
            item.Description = request.Description;

        if (request.EndDate.HasValue)
            item.EndDate = request.EndDate.Value;

        if (request.IsActive.HasValue)
            item.IsActive = request.IsActive.Value;

        if (request.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId.Value);
            if (account == null || account.UserId != userId)
                throw new InvalidOperationException("無權使用此帳戶");
            item.AccountId = request.AccountId.Value;
        }

        item.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.RecurringTransactions.Update(item);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(item);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var item = await _unitOfWork.RecurringTransactions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (item == null)
            return false;

        _unitOfWork.RecurringTransactions.Delete(item);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<int> ExecutePendingAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var pendingItems = await _context.RecurringTransactions
            .Where(r => r.UserId == userId && r.IsActive && r.NextOccurrenceDate <= today)
            .ToListAsync();

        var createdCount = 0;

        foreach (var item in pendingItems)
        {
            // 產生所有到期的交易（可能跨多期）
            while (item.NextOccurrenceDate <= today && item.IsActive)
            {
                // 建立交易
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                var category = await _unitOfWork.Categories.GetByIdAsync(item.CategoryId);

                if (user == null || category == null)
                    break;

                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = item.Amount,
                    Type = item.Type,
                    CategoryId = item.CategoryId,
                    Description = item.Description,
                    Date = item.NextOccurrenceDate,
                    AccountId = item.AccountId,
                    User = user,
                    Category = category,
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                createdCount++;

                // 計算下次執行日期
                item.NextOccurrenceDate = CalculateNextDate(item.NextOccurrenceDate, item.Frequency);

                // 超過結束日期，停用
                if (item.EndDate.HasValue && item.NextOccurrenceDate > item.EndDate.Value)
                {
                    item.IsActive = false;
                }

                item.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.RecurringTransactions.Update(item);
            }
        }

        if (createdCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return createdCount;
    }

    private static DateTime CalculateNextDate(DateTime current, RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(1),
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Monthly => current.AddMonths(1),
            RecurrenceFrequency.Yearly => current.AddYears(1),
            _ => current.AddMonths(1),
        };
    }

    private static RecurringTransactionResponse MapToResponse(RecurringTransaction item)
    {
        return new RecurringTransactionResponse
        {
            Id = item.Id,
            Amount = item.Amount,
            Type = item.Type,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            Description = item.Description,
            Frequency = item.Frequency,
            StartDate = item.StartDate,
            NextOccurrenceDate = item.NextOccurrenceDate,
            EndDate = item.EndDate,
            IsActive = item.IsActive,
            AccountId = item.AccountId,
            AccountName = item.Account?.Name,
            CreatedAt = item.CreatedAt,
        };
    }
}
