using FinFree.Api.Data;
using FinFree.Api.Models;

namespace FinFree.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<User>? _users;
    private IRepository<Transaction>? _transactions;
    private IRepository<Category>? _categories;
    private IRepository<Budget>? _budgets;
    private IRepository<RecurringTransaction>? _recurringTransactions;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Transaction> Transactions => _transactions ??= new Repository<Transaction>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Budget> Budgets => _budgets ??= new Repository<Budget>(_context);
    public IRepository<RecurringTransaction> RecurringTransactions => _recurringTransactions ??= new Repository<RecurringTransaction>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
