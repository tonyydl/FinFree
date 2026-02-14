using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class TransferRequest
{
    [Required(ErrorMessage = "來源帳戶為必填")]
    public int FromAccountId { get; set; }

    [Required(ErrorMessage = "目標帳戶為必填")]
    public int ToAccountId { get; set; }

    [Required(ErrorMessage = "金額為必填")]
    [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "日期為必填")]
    public DateTime Date { get; set; }

    [StringLength(500, ErrorMessage = "描述不可超過 500 字元")]
    public string? Description { get; set; }
}
