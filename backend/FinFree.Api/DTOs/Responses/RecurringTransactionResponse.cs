using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Responses;

public class RecurringTransactionResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime NextOccurrenceDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public DateTime CreatedAt { get; set; }
}
