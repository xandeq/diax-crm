using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.SurfAlerts;
using Diax.Application.SurfAlerts.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/surf-alerts")]
[Produces("application/json")]
public class SurfAlertsController : BaseApiController
{
    private readonly ISurfAlertService _service;

    public SurfAlertsController(ISurfAlertService service)
    {
        _service = service;
    }

    /// <summary>
    /// Recebe um alerta de janela de surf de uma automação externa.
    /// Header obrigatório: X-Alert-Api-Key com chave de escopo 'surf-alerts.ingest'.
    /// Header opcional: Idempotency-Key — reenvios com a mesma chave não duplicam.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IngestSurfAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IngestSurfAlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [RequestSizeLimit(64 * 1024)] // 64 KB
    public async Task<IActionResult> Ingest(
        [FromHeader(Name = "X-Alert-Api-Key")] string? apiKey,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] IngestSurfAlertRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { code = "SurfAlert.Unauthorized", message = "Header X-Alert-Api-Key obrigatório." });

        var result = await _service.IngestAsync(apiKey, idempotencyKey, request, ct);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("Unauthorized")) return Unauthorized(new { result.Error.Code, result.Error.Message });
            if (result.Error.Code.Contains("Forbidden")) return Forbid();
            if (result.Error.Code.Contains("RateLimited")) return StatusCode(429, new { result.Error.Code, result.Error.Message });
            return HandleResult(result);
        }

        return result.Value.Status == "duplicate"
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Lista os alertas de surf mais recentes. Leitura via sessão logada (JWT).
    /// </summary>
    [HttpGet]
    [Authorize]
    [RequirePermission("surf-alerts.view")]
    [ProducesResponseType(typeof(IReadOnlyList<SurfAlertResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _service.GetRecentAsync(limit, ct);
        return HandleResult(result);
    }
}
