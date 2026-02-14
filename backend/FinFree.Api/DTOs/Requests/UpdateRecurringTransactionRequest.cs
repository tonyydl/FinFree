using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class UpdateRecurringTransactionRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
    public decimal? Amount { get; set; }

    [StringLength(500, ErrorMessage = "描述不可超過 500 字元")]
    public string? Description { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public int? AccountId { get; set; }
}
