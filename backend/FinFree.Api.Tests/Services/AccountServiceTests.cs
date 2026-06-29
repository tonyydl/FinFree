using FinFree.Api.Data;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace FinFree.Api.Tests.Services;

public class AccountServiceTests
{
    [Fact]
    public async Task GetAllAsync_includes_initial_balance_when_calculating_balance()
    {
        await using var context = CreateContext();
        var user = CreateUser();
        var category = new Category { Id = 1, Name = "薪資", Type = TransactionType.Income };
        var account = new Account
        {
            Id = 1,
            UserId = user.Id,
            Name = "玉山銀行",
            AccountType = AccountType.Bank,
            InitialBalance = 1000m,
            User = user,
        };

        context.Users.Add(user);
        context.Categories.Add(category);
        context.Accounts.Add(account);
        context.Transactions.AddRange(
            new Transaction
            {
                UserId = user.Id,
                Amount = 500m,
                Type = TransactionType.Income,
                CategoryId = category.Id,
                Date = DateTime.UtcNow,
                AccountId = account.Id,
                User = user,
                Category = category,
                Account = account,
            },
            new Transaction
            {
                UserId = user.Id,
                Amount = 200m,
                Type = TransactionType.Expense,
                CategoryId = category.Id,
                Date = DateTime.UtcNow,
                AccountId = account.Id,
                User = user,
                Category = category,
                Account = account,
            });
        await context.SaveChangesAsync();

        var service = new AccountService(new UnitOfWork(context));

        var result = await service.GetAllAsync(user.Id);

        var response = Assert.Single(result);
        Assert.Equal(1000m, response.InitialBalance);
        Assert.Equal(1300m, response.Balance);
    }

    [Fact]
    public async Task TransferAsync_allows_transfer_funded_by_initial_balance()
    {
        await using var context = CreateContext();
        var user = CreateUser();
        var source = new Account
        {
            Id = 1,
            UserId = user.Id,
            Name = "現金",
            AccountType = AccountType.Cash,
            InitialBalance = 1000m,
            User = user,
        };
        var target = new Account
        {
            Id = 2,
            UserId = user.Id,
            Name = "銀行",
            AccountType = AccountType.Bank,
            InitialBalance = 0m,
            User = user,
        };

        context.Users.Add(user);
        context.Accounts.AddRange(source, target);
        await context.SaveChangesAsync();

        var service = new AccountService(new UnitOfWork(context));

        await service.TransferAsync(new TransferRequest
        {
            FromAccountId = source.Id,
            ToAccountId = target.Id,
            Amount = 600m,
            Date = DateTime.UtcNow,
        }, user.Id);

        var accounts = (await service.GetAllAsync(user.Id)).ToDictionary(a => a.Id);
        Assert.Equal(400m, accounts[source.Id].Balance);
        Assert.Equal(600m, accounts[target.Id].Balance);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static User CreateUser() => new()
    {
        Id = 1,
        Username = "tester",
        Email = "tester@example.com",
        PasswordHash = "hash",
    };
}
