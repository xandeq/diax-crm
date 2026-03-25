using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Application.Finance.Planner;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/personal-control")]
[Produces("application/json")]
public class PersonalFinanceController : BaseApiController
{
    private readonly PersonalFinanceControlService _monthService;
    private readonly TransactionService _transactionService;
    private readonly RecurringTransactionService _recurringService;
    private readonly DiaxDbContext _db;

    public PersonalFinanceController(
        PersonalFinanceControlService monthService,
        TransactionService transactionService,
        RecurringTransactionService recurringService,
        DiaxDbContext db)
    {
        _monthService = monthService;
        _transactionService = transactionService;
        _recurringService = recurringService;
        _db = db;
    }

    [HttpGet("month")]
    public async Task<IActionResult> GetCurrentMonth(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await GetMonth(now.Year, now.Month, cancellationToken);
    }

    [HttpGet("month/{year:int}/{month:int}")]
    public async Task<IActionResult> GetMonth(int year, int month, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _monthService.GetMonthAsync(year, month, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCurrentSummary(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await GetSummary(now.Year, now.Month, cancellationToken);
    }

    [HttpGet("summary/{year:int}/{month:int}")]
    public async Task<IActionResult> GetSummary(int year, int month, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _monthService.GetMonthAsync(year, month, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value.Summary) : BadRequest(result.Error);
    }

    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.CreateAsync(request, userId.Value, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMonth), new { year = request.Date.Year, month = request.Date.Month, version = "1" }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("transactions/{id}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.UpdateAsync(id, request, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("transactions/{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.DeleteAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("transactions/{id}/mark-paid")]
    public async Task<IActionResult> MarkTransactionAsPaid(Guid id, [FromBody] MarkTransactionPaidRequest? request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.MarkAsPaidAsync(id, userId.Value, request?.PaidDate, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("transactions/{id}/mark-pending")]
    public async Task<IActionResult> MarkTransactionAsPending(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.MarkAsPendingAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.GetAllAsync(userId.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("templates/active")]
    public async Task<IActionResult> GetActiveTemplates(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.GetActiveAsync(userId.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.GetActiveAsync(userId.Value);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var subscriptions = result.Value.Where(x => x.IsSubscription).ToList();
        return Ok(subscriptions);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateRecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.CreateAsync(request, userId.Value);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetTemplates), new { version = "1" }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateRecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.UpdateAsync(id, request, userId.Value);
        return HandleResult(result);
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.DeleteAsync(id, userId.Value);
        return HandleResult(result);
    }
}

public record MarkTransactionPaidRequest(DateTime? PaidDate = null);
