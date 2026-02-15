using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using FinFree.Api.Helpers;
using FinFree.Api.Models;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IConnectionMultiplexer _redis;

    public TokenService(IOptions<JwtSettings> jwtSettings, IConnectionMultiplexer redis)
    {
        _jwtSettings = jwtSettings.Value;
        _redis = redis;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task StoreTokenAsync(string jti, int userId, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"session:{jti}", userId.ToString(), expiry);
    }

    public async Task RevokeTokenAsync(string jti)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"session:{jti}");
    }

    public async Task<bool> IsTokenValidAsync(string jti)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"session:{jti}");
    }
}
