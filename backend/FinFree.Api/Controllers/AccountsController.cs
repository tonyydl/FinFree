using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinFree.Api.DTOs.Requests;
using FinFree.Api.Services.Interfaces;

namespace FinFree.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController : BaseController
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var accounts = await _service.GetAllAsync(userId);
        return Ok(accounts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var account = await _service.GetByIdAsync(id, userId);

        if (account == null)
            return NotFound(new { message = "帳戶不存在" });

        return Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var account = await _service.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var account = await _service.UpdateAsync(id, request, userId);

        if (account == null)
            return NotFound(new { message = "帳戶不存在" });

        return Ok(account);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        await _service.TransferAsync(request, userId);
        return Ok(new { message = "轉帳成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var result = await _service.DeleteAsync(id, userId);

        if (!result)
            return NotFound(new { message = "帳戶不存在" });

        return NoContent();
    }
}
