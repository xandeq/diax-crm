using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller unificado de categorias de transação.
/// Substitui IncomeCategoriesController + ExpenseCategoriesController.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/transaction-categories")]
[Produces("application/json")]
public class TransactionCategoriesController : BaseApiController
{
    private readonly TransactionCategoryService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<TransactionCategoriesController> _logger;

    public TransactionCategoriesController(TransactionCategoryService service, DiaxDbContext db, ILogger<TransactionCategoriesController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Lista apenas categorias ativas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetActiveAsync(userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Lista todas as categorias (ativas e inativas).
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetAllAsync(userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Lista categorias filtradas por tipo aplicável.
    /// Use ?applicableTo=1 (Income), 2 (Expense), 3 (Both)
    /// </summary>
    [HttpGet("by-type/{applicableTo}")]
    public async Task<IActionResult> GetByApplicableTo(CategoryApplicableTo applicableTo, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByApplicableToAsync(applicableTo, userId.Value, ct);
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionCategoryRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.CreateAsync(request, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionCategoryRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId.Value, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    /// <summary>
    /// Desativa (soft delete) uma categoria.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeactivateAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
