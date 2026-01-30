using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var transactions = await _transactionService.GetAllAsync(userId);
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var transaction = await _transactionService.GetByIdAsync(id, userId);

        if (transaction == null)
            return NotFound(new { message = "交易記錄不存在" });

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var transaction = await _transactionService.CreateAsync(request, userId);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var transaction = await _transactionService.UpdateAsync(id, request, userId);

        if (transaction == null)
            return NotFound(new { message = "交易記錄不存在" });

        return Ok(transaction);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var result = await _transactionService.DeleteAsync(id, userId);

        if (!result)
            return NotFound(new { message = "交易記錄不存在" });

        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var userId = GetUserId();
        var statistics = await _transactionService.GetStatisticsAsync(userId);
        return Ok(statistics);
    }
}
