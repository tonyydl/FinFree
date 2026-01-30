using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "使用者名稱為必填")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "使用者名稱長度需介於 3-50 字元")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Email 為必填")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "密碼為必填")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度需介於 6-100 字元")]
    public required string Password { get; set; }
}
