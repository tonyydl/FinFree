using FinFree.Api.Data;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinFree.Api.Tests.Services;

public class TransactionServiceTests
{
    [Fact]
    public async Task GetRangeReportAsync_includes_boundary_dates_and_excludes_out_of_range_transactions()
    {
        await using var context = CreateContext();
        var user = CreateUser();
        var salary = new Category { Id = 1, Name = "薪資", Type = TransactionType.Income };
        var food = new Category { Id = 2, Name = "飲食", Type = TransactionType.Expense };
        var transport = new Category { Id = 3, Name = "交通", Type = TransactionType.Expense };

        context.Users.Add(user);
        context.Categories.AddRange(salary, food, transport);
        context.Transactions.AddRange(
            CreateTransaction(user, salary, 999m, TransactionType.Income, new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)),
            CreateTransaction(user, salary, 5000m, TransactionType.Income, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateTransaction(user, food, 1200m, TransactionType.Expense, new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)),
            CreateTransaction(user, transport, 800m, TransactionType.Expense, new DateTime(2026, 6, 30, 23, 0, 0, DateTimeKind.Utc)),
            CreateTransaction(user, food, 777m, TransactionType.Expense, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var report = await service.GetRangeReportAsync(
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            user.Id);

        Assert.Equal(new DateTime(2026, 6, 1), report.StartDate);
        Assert.Equal(new DateTime(2026, 6, 30), report.EndDate);
        Assert.Equal(5000m, report.TotalIncome);
        Assert.Equal(2000m, report.TotalExpense);
        Assert.Equal(3000m, report.Balance);
        Assert.Equal(3, report.TransactionCount);
        Assert.Equal(3, report.DailyExpenses.Count);
        Assert.Contains(report.ExpenseByCategory, c =>
            c.CategoryName == "飲食" && c.Amount == 1200m && c.Percentage == 60m);
        Assert.Contains(report.ExpenseByCategory, c =>
            c.CategoryName == "交通" && c.Amount == 800m && c.Percentage == 40m);
    }

    private static TransactionService CreateService(AppDbContext context) =>
        new(new UnitOfWork(context), new MemoryCache(new MemoryCacheOptions()));

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

    private static Transaction CreateTransaction(
        User user,
        Category category,
        decimal amount,
        TransactionType type,
        DateTime date) => new()
        {
            UserId = user.Id,
            Amount = amount,
            Type = type,
            CategoryId = category.Id,
            Date = date,
            User = user,
            Category = category,
        };
}
