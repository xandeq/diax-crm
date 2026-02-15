using Asp.Versioning;
using Diax.Application.Finance.Planner;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/planner/goals")]
[Produces("application/json")]
public class FinancialGoalsController : BaseApiController
{
    private readonly FinancialGoalService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<FinancialGoalsController> _logger;

    public FinancialGoalsController(
        FinancialGoalService service,
        DiaxDbContext db,
        ILogger<FinancialGoalsController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todas as metas financeiras do usuário
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetAllAsync(userId.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Obtém apenas as metas ativas
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetActiveGoalsAsync(userId.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Obtém uma meta específica por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByIdAsync(id, userId.Value);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Cria uma nova meta financeira
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.CreateAsync(request, userId.Value);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id, version = "1" }, result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Atualiza uma meta existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinancialGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Adiciona uma contribuição à meta
    /// </summary>
    [HttpPost("{id}/contribute")]
    public async Task<IActionResult> AddContribution(Guid id, [FromBody] AddContributionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.AddContributionAsync(id, request.Amount, userId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Exclui uma meta financeira
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(id, userId.Value);
        return HandleResult(result);
    }
}
