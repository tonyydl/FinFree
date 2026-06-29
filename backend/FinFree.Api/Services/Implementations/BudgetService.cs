using Microsoft.EntityFrameworkCore;
using FinFree.Api.Data;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class BudgetService : IBudgetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public BudgetService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<IEnumerable<BudgetResponse>> GetByMonthAsync(int userId, int year, int month)
    {
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .OrderBy(b => b.CategoryId == null ? 0 : 1)
            .ThenBy(b => b.Category != null ? b.Category.Name : "")
            .ToListAsync();

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        // 查詢該月支出
        var expenses = await _context.Transactions
            .Where(t => t.UserId == userId
                && t.Type == TransactionType.Expense
                && t.Date >= startDate
                && t.Date < endDate)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        var totalSpent = expenses.Sum(e => e.Total);

        return budgets.Select(b => new BudgetResponse
        {
            Id = b.Id,
            CategoryId = b.CategoryId,
            CategoryName = b.Category?.Name,
            Amount = b.Amount,
            Spent = b.CategoryId == null
                ? totalSpent
                : expenses.FirstOrDefault(e => e.CategoryId == b.CategoryId)?.Total ?? 0,
            Year = b.Year,
            Month = b.Month,
        });
    }

    public async Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, int userId)
    {
        // 檢查同月份同分類是否已有預算
        var exists = await _unitOfWork.Budgets.ExistsAsync(b =>
            b.UserId == userId
            && b.Year == request.Year
            && b.Month == request.Month
            && b.CategoryId == request.CategoryId);

        if (exists)
            throw new InvalidOperationException("此月份該分類已設定預算");

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Year = request.Year,
            Month = request.Month,
        };

        await _unitOfWork.Budgets.AddAsync(budget);
        await _unitOfWork.SaveChangesAsync();

        // 取得分類名稱
        string? categoryName = null;
        if (budget.CategoryId != null)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId.Value);
            categoryName = category?.Name;
        }

        return new BudgetResponse
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = categoryName,
            Amount = budget.Amount,
            Spent = 0,
            Year = budget.Year,
            Month = budget.Month,
        };
    }

    public async Task<IEnumerable<BudgetResponse>> CopyPreviousMonthAsync(int userId, int year, int month)
    {
        if (year < 2020 || year > 2100 || month < 1 || month > 12)
            throw new InvalidOperationException("無效的年份或月份");

        var targetHasBudgets = await _context.Budgets
            .AnyAsync(b => b.UserId == userId && b.Year == year && b.Month == month);

        if (targetHasBudgets)
            throw new InvalidOperationException("本月已有預算，無法複製上月預算");

        var previous = new DateTime(year, month, 1).AddMonths(-1);
        var previousBudgets = await _context.Budgets
            .Where(b => b.UserId == userId && b.Year == previous.Year && b.Month == previous.Month)
            .OrderBy(b => b.CategoryId == null ? 0 : 1)
            .ThenBy(b => b.CategoryId)
            .ToListAsync();

        if (previousBudgets.Count == 0)
            throw new InvalidOperationException("上月沒有可複製的預算");

        var copied = previousBudgets.Select(b => new Budget
        {
            UserId = userId,
            CategoryId = b.CategoryId,
            Amount = b.Amount,
            Year = year,
            Month = month,
        });

        await _context.Budgets.AddRangeAsync(copied);
        await _context.SaveChangesAsync();

        return await GetByMonthAsync(userId, year, month);
    }

    public async Task<BudgetResponse?> UpdateAsync(int id, UpdateBudgetRequest request, int userId)
    {
        var budget = await _unitOfWork.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return null;

        budget.Amount = request.Amount;
        budget.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Budgets.Update(budget);
        await _unitOfWork.SaveChangesAsync();

        // 取得分類名稱和花費
        string? categoryName = null;
        if (budget.CategoryId != null)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId.Value);
            categoryName = category?.Name;
        }

        var startDate = new DateTime(budget.Year, budget.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        decimal spent;
        if (budget.CategoryId == null)
        {
            spent = await _context.Transactions
                .Where(t => t.UserId == userId && t.Type == TransactionType.Expense
                    && t.Date >= startDate && t.Date < endDate)
                .SumAsync(t => t.Amount);
        }
        else
        {
            spent = await _context.Transactions
                .Where(t => t.UserId == userId && t.Type == TransactionType.Expense
                    && t.CategoryId == budget.CategoryId
                    && t.Date >= startDate && t.Date < endDate)
                .SumAsync(t => t.Amount);
        }

        return new BudgetResponse
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = categoryName,
            Amount = budget.Amount,
            Spent = spent,
            Year = budget.Year,
            Month = budget.Month,
        };
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var budget = await _unitOfWork.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return false;

        _unitOfWork.Budgets.Delete(budget);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
