namespace FinFree.Api.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TransactionType Type { get; set; }
    public int? UserId { get; set; }  // null = 系統預設分類
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
