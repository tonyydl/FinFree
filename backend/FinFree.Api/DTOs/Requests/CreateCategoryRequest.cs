using System.ComponentModel.DataAnnotations;
using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Requests;

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "分類名稱為必填")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "分類名稱長度須在 1-50 字元之間")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "分類類型為必填")]
    public TransactionType Type { get; set; }
}
