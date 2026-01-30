using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // 檢查 Email 是否已存在
        if (await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email))
        {
            return null;
        }

        // 檢查使用者名稱是否已存在
        if (await _unitOfWork.Users.ExistsAsync(u => u.Username == request.Username))
        {
            return null;
        }

        // 建立新使用者
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // 產生 JWT Token
        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // 尋找使用者
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return null;
        }

        // 驗證密碼
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        // 產生 JWT Token
        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        };
    }
}
