using FinFree.Api.Data;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace FinFree.Api.Tests.Services;

public class BudgetServiceTests
{
    [Fact]
    public async Task CopyPreviousMonthAsync_creates_matching_budgets_in_target_month()
    {
        await using var context = CreateContext();
        var user = CreateUser();
        var food = new Category { Id = 1, Name = "飲食", Type = TransactionType.Expense };
        var transport = new Category { Id = 2, Name = "交通", Type = TransactionType.Expense };

        context.Users.Add(user);
        context.Categories.AddRange(food, transport);
        context.Budgets.AddRange(
            new Budget { UserId = user.Id, CategoryId = null, Amount = 30000m, Year = 2026, Month = 4 },
            new Budget { UserId = user.Id, CategoryId = food.Id, Amount = 12000m, Year = 2026, Month = 4 },
            new Budget { UserId = user.Id, CategoryId = transport.Id, Amount = 3000m, Year = 2026, Month = 4 });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var copied = (await service.CopyPreviousMonthAsync(user.Id, 2026, 5)).ToList();

        Assert.Equal(3, copied.Count);
        Assert.All(copied, b =>
        {
            Assert.Equal(2026, b.Year);
            Assert.Equal(5, b.Month);
            Assert.Equal(0m, b.Spent);
        });
        Assert.Contains(copied, b => b.CategoryId == null && b.Amount == 30000m);
        Assert.Contains(copied, b => b.CategoryId == food.Id && b.Amount == 12000m);
        Assert.Contains(copied, b => b.CategoryId == transport.Id && b.Amount == 3000m);
    }

    [Fact]
    public async Task CopyPreviousMonthAsync_rejects_target_month_that_already_has_budgets()
    {
        await using var context = CreateContext();
        var user = CreateUser();

        context.Users.Add(user);
        context.Budgets.AddRange(
            new Budget { UserId = user.Id, CategoryId = null, Amount = 30000m, Year = 2026, Month = 4 },
            new Budget { UserId = user.Id, CategoryId = null, Amount = 32000m, Year = 2026, Month = 5 });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CopyPreviousMonthAsync(user.Id, 2026, 5));
        Assert.Equal("本月已有預算，無法複製上月預算", ex.Message);
    }

    [Fact]
    public async Task CopyPreviousMonthAsync_rejects_when_previous_month_has_no_budgets()
    {
        await using var context = CreateContext();
        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CopyPreviousMonthAsync(user.Id, 2026, 5));
        Assert.Equal("上月沒有可複製的預算", ex.Message);
    }

    private static BudgetService CreateService(AppDbContext context) =>
        new(new UnitOfWork(context), context);

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
