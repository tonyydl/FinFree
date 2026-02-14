using FinFree.Api.Models;

namespace FinFree.Api.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<Category> Categories { get; }
    IRepository<Budget> Budgets { get; }
    IRepository<RecurringTransaction> RecurringTransactions { get; }
    IRepository<Account> Accounts { get; }
    Task<int> SaveChangesAsync();
}
