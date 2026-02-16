using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Requests;

public class TransactionQueryParams
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Keyword { get; set; }
    public TransactionType? Type { get; set; }
    public int? AccountId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
