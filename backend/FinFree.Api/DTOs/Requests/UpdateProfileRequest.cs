using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "使用者名稱為必填")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "使用者名稱長度須在 2-50 字元之間")]
    public required string Username { get; set; }
}
