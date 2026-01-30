using Microsoft.AspNetCore.Mvc;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(request);

        if (result == null)
            return BadRequest(new { message = "使用者名稱或 Email 已存在" });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request);

        if (result == null)
            return Unauthorized(new { message = "Email 或密碼錯誤" });

        return Ok(result);
    }
}
