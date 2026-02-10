using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "目前密碼為必填")]
    public required string CurrentPassword { get; set; }

    [Required(ErrorMessage = "新密碼為必填")]
    [MinLength(6, ErrorMessage = "新密碼至少需要 6 個字元")]
    public required string NewPassword { get; set; }
}
