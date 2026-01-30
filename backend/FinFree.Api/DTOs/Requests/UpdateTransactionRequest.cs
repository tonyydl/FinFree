using System.ComponentModel.DataAnnotations;
using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Requests;

public class UpdateTransactionRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
    public decimal? Amount { get; set; }

    public TransactionType? Type { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "描述不可超過 500 字元")]
    public string? Description { get; set; }

    public DateTime? Date { get; set; }
}
