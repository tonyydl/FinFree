using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountResponse>> GetAllAsync(int userId);
    Task<AccountResponse?> GetByIdAsync(int id, int userId);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, int userId);
    Task<AccountResponse?> UpdateAsync(int id, UpdateAccountRequest request, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task TransferAsync(TransferRequest request, int userId);
}
