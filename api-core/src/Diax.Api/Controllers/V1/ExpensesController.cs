using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Obsolete("Use TransactionsController instead. This controller will be removed in a future version.")]
public class ExpensesController : BaseApiController
{
    private readonly ExpenseService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(ExpenseService service, DiaxDbContext db, ILogger<ExpensesController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ExpensePagedRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetPagedAsync(request, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByIdAsync(id, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("month/{year}/{month}")]
    public async Task<IActionResult> GetByMonth(int year, int month, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByMonthAsync(year, month, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.CreateAsync(request, userId.Value, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1" }, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest? request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[BulkDelete] Iniciado com {Count} IDs", request?.Ids?.Count ?? 0);

        if (request == null || request.Ids == null || !request.Ids.Any())
        {
            _logger.LogWarning("[BulkDelete] Request vazio ou nulo");
            return BadRequest(new { error = "General.EmptyRequest", message = "Nenhum ID foi fornecido" });
        }

        _logger.LogDebug("[BulkDelete] IDs recebidos: {Ids}", string.Join(", ", request.Ids.Take(5)));
        var invalidIds = request.Ids.Where(id => id == Guid.Empty).ToList();
        if (invalidIds.Any())
        {
            _logger.LogWarning("[BulkDelete] {Count} IDs inválidos (Guid.Empty) detectados", invalidIds.Count);
            return BadRequest(new { error = "General.InvalidIds", message = $"{invalidIds.Count} IDs inválidos (vazios) foram enviados" });
        }

        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue)
        {
            _logger.LogWarning("[BulkDelete] Unauthorized - UserId não resolvido");
            return Unauthorized();
        }

        _logger.LogDebug("[BulkDelete] UserId resolvido: {UserId}", userId.Value);

        var result = await _service.DeleteRangeAsync(request, userId.Value, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("[BulkDelete] Sucesso - {DeletedCount} despesas excluídas", result.Value.DeletedCount);
        }
        else
        {
            _logger.LogWarning("[BulkDelete] Falha - {ErrorCode}: {ErrorMessage}", result.Error.Code, result.Error.Message);
        }

        return result.IsSuccess ? Ok(result.Value) : HandleResult(result);
    }
}
