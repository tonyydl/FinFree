using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class UpdateBudgetRequest
{
    [Required(ErrorMessage = "金額為必填")]
    [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }
}
