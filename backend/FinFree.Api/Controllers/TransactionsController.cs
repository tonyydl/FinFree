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

    [HttpGet("report")]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest(new { message = "無效的年份或月份" });

        var userId = GetUserId();
        var report = await _transactionService.GetMonthlyReportAsync(year, month, userId);
        return Ok(report);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var userId = GetUserId();
        var statistics = await _transactionService.GetStatisticsAsync(userId);
        return Ok(statistics);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromForm] IFormFile file, [FromForm] int accountId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "請選擇 CSV 檔案" });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "僅支援 CSV 格式" });

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var (imported, failed, errors) = await _transactionService.ImportCsvAsync(stream, accountId, userId);

        return Ok(new { imported, failed, errors });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var userId = GetUserId();
        var csvBytes = await _transactionService.ExportCsvAsync(userId);
        var fileName = $"FinFree_交易記錄_{DateTime.Now:yyyyMMdd}.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}
