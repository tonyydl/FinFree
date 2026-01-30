using FinFree.Api.Models;

namespace FinFree.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
