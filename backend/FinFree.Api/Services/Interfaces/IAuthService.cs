using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
