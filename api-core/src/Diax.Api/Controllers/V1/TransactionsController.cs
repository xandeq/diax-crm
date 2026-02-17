using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller unificado de transações financeiras.
/// Substitui IncomesController + ExpensesController.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class TransactionsController : BaseApiController
{
    private readonly TransactionService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(TransactionService service, DiaxDbContext db, ILogger<TransactionsController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Lista transações com paginação e filtros.
    /// Use ?type=1 (Income), 2 (Expense), 3 (Transfer), 4 (Ignored)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TransactionPagedRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetPagedAsync(request, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByIdAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetByType(TransactionType type, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByTypeAsync(type, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("month/{year}/{month}")]
    public async Task<IActionResult> GetByMonth(int year, int month, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByMonthAsync(year, month, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(TransactionStatus status, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByStatusAsync(status, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.CreateAsync(request, userId.Value, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1" }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId.Value, ct);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(id, userId.Value, ct);
        return HandleResult(result);
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest? request, CancellationToken ct)
    {
        _logger.LogInformation("[Transactions.BulkDelete] Iniciado com {Count} IDs", request?.Ids?.Count ?? 0);

        if (request == null || request.Ids == null || !request.Ids.Any())
            return BadRequest(new { error = "General.EmptyRequest", message = "Nenhum ID foi fornecido" });

        var invalidIds = request.Ids.Where(id => id == Guid.Empty).ToList();
        if (invalidIds.Any())
            return BadRequest(new { error = "General.InvalidIds", message = $"{invalidIds.Count} IDs inválidos (vazios) foram enviados" });

        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteRangeAsync(request, userId.Value, ct);

        if (result.IsSuccess)
            _logger.LogInformation("[Transactions.BulkDelete] Sucesso - {DeletedCount} excluídas", result.Value.DeletedCount);
        else
            _logger.LogWarning("[Transactions.BulkDelete] Falha - {ErrorCode}: {ErrorMessage}", result.Error.Code, result.Error.Message);

        return result.IsSuccess ? Ok(result.Value) : HandleResult(result);
    }

    /// <summary>
    /// Reclassifica o tipo financeiro de uma transação.
    /// Ex: Mudar de Income → Transfer, ou Expense → Ignored.
    /// </summary>
    [HttpPatch("{id}/reclassify")]
    public async Task<IActionResult> Reclassify(Guid id, [FromBody] ReclassifyTransactionRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.ReclassifyAsync(id, request, userId.Value, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Marca uma transação como paga.
    /// </summary>
    [HttpPatch("{id}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(Guid id, [FromBody] MarkPaidRequest? request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.MarkAsPaidAsync(id, userId.Value, request?.PaidDate, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Marca uma transação como pendente.
    /// </summary>
    [HttpPatch("{id}/mark-pending")]
    public async Task<IActionResult> MarkAsPending(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.MarkAsPendingAsync(id, userId.Value, ct);
        return HandleResult(result);
    }
}

/// <summary>
/// Request simples para o endpoint MarkAsPaid
/// </summary>
public record MarkPaidRequest(DateTime? PaidDate = null);
