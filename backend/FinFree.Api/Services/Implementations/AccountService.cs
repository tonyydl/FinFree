using Microsoft.EntityFrameworkCore;
using FinFree.Api.Data;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public AccountService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<IEnumerable<AccountResponse>> GetAllAsync(int userId)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var accountIds = accounts.Select(a => a.Id).ToList();

        // 計算各帳戶餘額（收入 - 支出）
        var balances = await _context.Transactions
            .Where(t => t.UserId == userId && t.AccountId != null && accountIds.Contains(t.AccountId.Value))
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount)
            })
            .ToDictionaryAsync(x => x.AccountId!.Value, x => x.Balance);

        return accounts.Select(a => new AccountResponse
        {
            Id = a.Id,
            Name = a.Name,
            Balance = balances.GetValueOrDefault(a.Id, 0),
            CreatedAt = a.CreatedAt,
        });
    }

    public async Task<AccountResponse?> GetByIdAsync(int id, int userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return null;

        var balance = await _context.Transactions
            .Where(t => t.AccountId == id && t.UserId == userId)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            Balance = balance,
            CreatedAt = account.CreatedAt,
        };
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, int userId)
    {
        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
        };

        await _unitOfWork.Accounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            Balance = 0,
            CreatedAt = account.CreatedAt,
        };
    }

    public async Task<AccountResponse?> UpdateAsync(int id, UpdateAccountRequest request, int userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return null;

        account.Name = request.Name;
        account.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Accounts.Update(account);
        await _unitOfWork.SaveChangesAsync();

        var balance = await _context.Transactions
            .Where(t => t.AccountId == id && t.UserId == userId)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            Balance = balance,
            CreatedAt = account.CreatedAt,
        };
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return false;

        // 有交易記錄的帳戶不可刪除
        var hasTransactions = await _context.Transactions
            .AnyAsync(t => t.AccountId == id);

        if (hasTransactions)
            throw new InvalidOperationException("此帳戶仍有交易記錄，無法刪除");

        _unitOfWork.Accounts.Delete(account);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
