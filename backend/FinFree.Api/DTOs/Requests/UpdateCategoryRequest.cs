using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "分類名稱為必填")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "分類名稱長度須在 1-50 字元之間")]
    public required string Name { get; set; }
}
