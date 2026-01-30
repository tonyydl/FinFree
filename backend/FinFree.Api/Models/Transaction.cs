namespace FinFree.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public required User User { get; set; }
    public required Category Category { get; set; }
}
