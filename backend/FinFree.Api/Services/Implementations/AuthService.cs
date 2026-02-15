using System.IdentityModel.Tokens.Jwt;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using FinFree.Api.Helpers;

namespace FinFree.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email))
            return null;

        if (await _unitOfWork.Users.ExistsAsync(u => u.Username == request.Username))
            return null;

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        await StoreTokenInRedisAsync(token, user.Id);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = _tokenService.GenerateToken(user);
        await StoreTokenInRedisAsync(token, user.Id);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        };
    }

    public async Task LogoutAsync(string jti)
    {
        await _tokenService.RevokeTokenAsync(jti);
    }

    private async Task StoreTokenInRedisAsync(string token, int userId)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var jti = jwtToken.Id;
        var expiry = TimeSpan.FromMinutes(_jwtSettings.ExpiryInMinutes);
        await _tokenService.StoreTokenAsync(jti, userId, expiry);
    }
}
