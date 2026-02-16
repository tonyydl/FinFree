using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Requests;

public class RecurringTransactionQueryParams
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public RecurrenceFrequency? Frequency { get; set; }
    public bool? IsActive { get; set; }
}
