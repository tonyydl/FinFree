using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinFree.Api.Data;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == null) // 只取得系統預設分類
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                IsSystemDefault = c.UserId == null
            })
            .ToListAsync();

        return Ok(categories);
    }
}
