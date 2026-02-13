using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet("{year}/{month}")]
    public async Task<IActionResult> GetByMonth(int year, int month)
    {
        var userId = GetUserId();
        var budgets = await _budgetService.GetByMonthAsync(userId, year, month);
        return Ok(budgets);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        try
        {
            var budget = await _budgetService.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetByMonth),
                new { year = budget.Year, month = budget.Month }, budget);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBudgetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var budget = await _budgetService.UpdateAsync(id, request, userId);

        if (budget == null)
            return NotFound(new { message = "預算不存在或無權限修改" });

        return Ok(budget);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var result = await _budgetService.DeleteAsync(id, userId);

        if (!result)
            return NotFound(new { message = "預算不存在或無權限刪除" });

        return NoContent();
    }
}
