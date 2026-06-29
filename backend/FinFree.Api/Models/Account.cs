namespace FinFree.Api.Models;

public enum AccountType
{
    Cash = 0,       // 現金
    Bank = 1,       // 銀行
    CreditCard = 2, // 信用卡
    Investment = 3, // 投資
    Other = 4,      // 其他
}

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Name { get; set; }
    public AccountType AccountType { get; set; } = AccountType.Bank;
    public decimal InitialBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
}
