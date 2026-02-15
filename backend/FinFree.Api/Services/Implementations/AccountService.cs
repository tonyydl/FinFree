using Microsoft.EntityFrameworkCore;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AccountResponse>> GetAllAsync(int userId)
    {
        var accounts = await _unitOfWork.Accounts.Query()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var accountIds = accounts.Select(a => a.Id).ToList();

        var balances = await _unitOfWork.Transactions.Query()
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
            AccountType = a.AccountType,
            Balance = balances.GetValueOrDefault(a.Id, 0),
            CreatedAt = a.CreatedAt,
        });
    }

    public async Task<AccountResponse?> GetByIdAsync(int id, int userId)
    {
        var account = await _unitOfWork.Accounts.Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return null;

        var balance = await _unitOfWork.Transactions.Query()
            .Where(t => t.AccountId == id && t.UserId == userId)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            AccountType = account.AccountType,
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
            AccountType = request.AccountType,
        };

        await _unitOfWork.Accounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            AccountType = account.AccountType,
            Balance = 0,
            CreatedAt = account.CreatedAt,
        };
    }

    public async Task<AccountResponse?> UpdateAsync(int id, UpdateAccountRequest request, int userId)
    {
        var account = await _unitOfWork.Accounts.Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return null;

        account.Name = request.Name;
        account.AccountType = request.AccountType;
        account.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Accounts.Update(account);
        await _unitOfWork.SaveChangesAsync();

        var balance = await _unitOfWork.Transactions.Query()
            .Where(t => t.AccountId == id && t.UserId == userId)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            AccountType = account.AccountType,
            Balance = balance,
            CreatedAt = account.CreatedAt,
        };
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var account = await _unitOfWork.Accounts.Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
            return false;

        var hasTransactions = await _unitOfWork.Transactions.ExistsAsync(t => t.AccountId == id);
        if (hasTransactions)
            throw new InvalidOperationException("此帳戶仍有交易記錄，無法刪除");

        _unitOfWork.Accounts.Delete(account);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private const string TransferInCategoryName = "轉帳收入";
    private const string TransferOutCategoryName = "轉帳支出";

    private async Task<Category> GetOrCreateSystemCategoryAsync(string name, TransactionType type)
    {
        var category = await _unitOfWork.Categories.Query()
            .FirstOrDefaultAsync(c => c.Name == name && c.UserId == null);

        if (category != null)
            return category;

        category = new Category
        {
            Name = name,
            Type = type,
            UserId = null,
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return category;
    }

    public async Task TransferAsync(DTOs.Requests.TransferRequest request, int userId)
    {
        if (request.FromAccountId == request.ToAccountId)
            throw new InvalidOperationException("來源帳戶與目標帳戶不能相同");

        var fromAccount = await _unitOfWork.Accounts.Query()
            .FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId);
        var toAccount = await _unitOfWork.Accounts.Query()
            .FirstOrDefaultAsync(a => a.Id == request.ToAccountId && a.UserId == userId);

        if (fromAccount == null || toAccount == null)
            throw new InvalidOperationException("帳戶不存在");

        var fromBalance = await _unitOfWork.Transactions.Query()
            .Where(t => t.AccountId == request.FromAccountId && t.UserId == userId)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

        if (fromBalance < request.Amount)
            throw new InvalidOperationException("來源帳戶餘額不足");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("系統資料異常");

        var outCategory = await GetOrCreateSystemCategoryAsync(TransferOutCategoryName, TransactionType.Expense);
        var inCategory = await GetOrCreateSystemCategoryAsync(TransferInCategoryName, TransactionType.Income);

        var description = request.Description ?? $"從「{fromAccount.Name}」轉至「{toAccount.Name}」";

        var outTransaction = new Transaction
        {
            UserId = userId,
            Amount = request.Amount,
            Type = TransactionType.Expense,
            CategoryId = outCategory.Id,
            Description = description,
            Date = request.Date,
            AccountId = request.FromAccountId,
            User = user,
            Category = outCategory,
        };

        var inTransaction = new Transaction
        {
            UserId = userId,
            Amount = request.Amount,
            Type = TransactionType.Income,
            CategoryId = inCategory.Id,
            Description = description,
            Date = request.Date,
            AccountId = request.ToAccountId,
            User = user,
            Category = inCategory,
        };

        await _unitOfWork.Transactions.AddAsync(outTransaction);
        await _unitOfWork.Transactions.AddAsync(inTransaction);
        await _unitOfWork.SaveChangesAsync();
    }
}
