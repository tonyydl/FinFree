using FinFree.Api.Models;

namespace FinFree.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    Task StoreTokenAsync(string jti, int userId, TimeSpan expiry);
    Task RevokeTokenAsync(string jti);
    Task<bool> IsTokenValidAsync(string jti);
}
