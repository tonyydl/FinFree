using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class CreateBudgetRequest
{
    public int? CategoryId { get; set; }  // null = 整體預算

    [Required(ErrorMessage = "金額為必填")]
    [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "年份為必填")]
    [Range(2020, 2100)]
    public int Year { get; set; }

    [Required(ErrorMessage = "月份為必填")]
    [Range(1, 12, ErrorMessage = "月份必須在 1-12 之間")]
    public int Month { get; set; }
}
