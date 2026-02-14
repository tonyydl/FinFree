using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface IRecurringTransactionService
{
    Task<IEnumerable<RecurringTransactionResponse>> GetAllAsync(int userId);
    Task<RecurringTransactionResponse?> GetByIdAsync(int id, int userId);
    Task<RecurringTransactionResponse> CreateAsync(CreateRecurringTransactionRequest request, int userId);
    Task<RecurringTransactionResponse?> UpdateAsync(int id, UpdateRecurringTransactionRequest request, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<int> ExecutePendingAsync(int userId);
}
