namespace FinFree.Api.Models;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
}
