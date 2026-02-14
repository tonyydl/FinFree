using System.ComponentModel.DataAnnotations;
using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Requests;

public class CreateTransactionRequest
{
    [Required(ErrorMessage = "金額為必填")]
    [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "交易類型為必填")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "分類為必填")]
    public int CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "描述不可超過 500 字元")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "日期為必填")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "帳戶為必填")]
    public int AccountId { get; set; }
}
