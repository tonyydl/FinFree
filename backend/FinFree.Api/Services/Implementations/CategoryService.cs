using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;
using FinFree.Api.Models;
using FinFree.Api.Repositories;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync(int userId)
    {
        // 取得系統預設分類 (UserId == null) + 使用者自訂分類
        var categories = await _unitOfWork.Categories
            .FindAsync(c => c.UserId == null || c.UserId == userId);

        return categories
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Id)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                IsSystemDefault = c.UserId == null
            });
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, int userId)
    {
        var category = new Category
        {
            Name = request.Name,
            Type = request.Type,
            UserId = userId,
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            IsSystemDefault = false
        };
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request, int userId)
    {
        var category = await _unitOfWork.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return null;

        category.Name = request.Name;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            IsSystemDefault = false
        };
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var category = await _unitOfWork.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return false;

        // 檢查是否有交易記錄使用此分類
        var hasTransactions = await _unitOfWork.Transactions
            .ExistsAsync(t => t.CategoryId == id);

        if (hasTransactions)
            throw new InvalidOperationException("此分類下有交易記錄，無法刪除");

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
