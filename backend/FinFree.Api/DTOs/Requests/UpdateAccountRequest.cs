using System.ComponentModel.DataAnnotations;

namespace FinFree.Api.DTOs.Requests;

public class UpdateAccountRequest
{
    [Required(ErrorMessage = "帳戶名稱為必填")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "帳戶名稱長度須為 1-50 字")]
    public required string Name { get; set; }
}
