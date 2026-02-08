using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllAsync(int userId);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, int userId);
    Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
