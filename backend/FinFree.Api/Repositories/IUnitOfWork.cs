using FinFree.Api.Models;

namespace FinFree.Api.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<Category> Categories { get; }
    Task<int> SaveChangesAsync();
}
