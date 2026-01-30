using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class LoginRequest
{
    [Required(ErrorMessage = "Email 為必填")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "密碼為必填")]
    public required string Password { get; set; }
}
