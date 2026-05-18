using Asp.Versioning;
using Diax.Application.Finance.Planner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/planner/simulations")]
[Produces("application/json")]
public class MonthlySimulationsController : BaseApiController
{
    private readonly MonthlySimulationService _service;
    private readonly ILogger<MonthlySimulationsController> _logger;

    public MonthlySimulationsController(
        MonthlySimulationService service,
        ILogger<MonthlySimulationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtém ou gera a simulação para um mês específico
    /// </summary>
    [HttpGet("{year}/{month}")]
    public async Task<IActionResult> GetOrGenerate(int year, int month, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        if (month < 1 || month > 12)
            return BadRequest(new { error = "Invalid month. Must be between 1 and 12." });

        var result = await _service.GetOrGenerateSimulationAsync(month, year, userId.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Força a geração de uma nova simulação (sobrescreve existente)
    /// </summary>
    [HttpPost("{year}/{month}/regenerate")]
    public async Task<IActionResult> Regenerate(int year, int month, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        if (month < 1 || month > 12)
            return BadRequest(new { error = "Invalid month. Must be between 1 and 12." });

        var result = await _service.GenerateSimulationAsync(month, year, userId.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
