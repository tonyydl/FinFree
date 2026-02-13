using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetResponse>> GetByMonthAsync(int userId, int year, int month);
    Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, int userId);
    Task<BudgetResponse?> UpdateAsync(int id, UpdateBudgetRequest request, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
