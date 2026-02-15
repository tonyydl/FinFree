using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Controllers;

[ApiController]
[Route("api/recurring-transactions")]
[Authorize]
public class RecurringTransactionsController : BaseController
{
    private readonly IRecurringTransactionService _service;

    public RecurringTransactionsController(IRecurringTransactionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var items = await _service.GetAllAsync(userId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var item = await _service.GetByIdAsync(id, userId);

        if (item == null)
            return NotFound(new { message = "定期交易不存在" });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var item = await _service.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecurringTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var item = await _service.UpdateAsync(id, request, userId);

        if (item == null)
            return NotFound(new { message = "定期交易不存在" });

        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var result = await _service.DeleteAsync(id, userId);

        if (!result)
            return NotFound(new { message = "定期交易不存在" });

        return NoContent();
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute()
    {
        var userId = GetUserId();
        var count = await _service.ExecutePendingAsync(userId);
        return Ok(new { createdCount = count });
    }
}
