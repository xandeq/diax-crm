using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.Auth;
using Diax.Application.SurfAlerts;
using Diax.Application.SurfAlerts.Dtos;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/surf-alerts")]
[Produces("application/json")]
public class SurfAlertsController : BaseApiController
{
    private const string ViewPermission = "surf-alerts.view";

    private readonly ISurfAlertService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissionService;

    public SurfAlertsController(
        ISurfAlertService service,
        ICurrentUserService currentUser,
        IPermissionService permissionService)
    {
        _service = service;
        _currentUser = currentUser;
        _permissionService = permissionService;
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
    /// Lista os alertas de surf mais recentes. Aceita sessão logada (JWT, painel) OU
    /// header X-Alert-Api-Key com escopo 'surf-alerts.view' (automações/scripts).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SurfAlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecent(
        [FromHeader(Name = "X-Alert-Api-Key")] string? apiKey,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<SurfAlertResponse>> result;

        if (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
        {
            if (!await _permissionService.HasPermissionAsync(_currentUser.UserId.Value, ViewPermission, ct))
                return Forbid();

            result = await _service.GetRecentAsync(null, limit, ct);
        }
        else if (!string.IsNullOrWhiteSpace(apiKey))
        {
            result = await _service.GetRecentAsync(apiKey, limit, ct);
        }
        else
        {
            return Unauthorized(new { code = "SurfAlert.Unauthorized", message = "Sessão logada (JWT) ou header X-Alert-Api-Key obrigatórios." });
        }

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("Unauthorized")) return Unauthorized(new { result.Error.Code, result.Error.Message });
            if (result.Error.Code.Contains("Forbidden")) return Forbid();
        }

        return HandleResult(result);
    }
}
