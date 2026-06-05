using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.ErrorLogs;
using Diax.Application.ErrorLogs.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/error-logs")]
[Produces("application/json")]
public class ErrorLogsController : BaseApiController
{
    private readonly IErrorLogService _service;
    private readonly ILogger<ErrorLogsController> _logger;

    public ErrorLogsController(IErrorLogService service, ILogger<ErrorLogsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ===== Ingestão (autenticação por API Key, sem JWT) =====

    /// <summary>
    /// Recebe um único log de erro de qualquer aplicação.
    /// Header obrigatório: X-Log-Api-Key com chave de escopo 'error-logs.ingest'.
    /// </summary>
    [HttpPost("ingest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [RequestSizeLimit(32 * 1024)] // 32 KB
    public async Task<IActionResult> Ingest(
        [FromHeader(Name = "X-Log-Api-Key")] string? apiKey,
        [FromBody] IngestErrorLogRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { code = "ErrorLog.Unauthorized", message = "Header X-Log-Api-Key obrigatório." });

        var result = await _service.IngestAsync(apiKey, request, ct);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("Unauthorized")) return Unauthorized(new { result.Error.Code, result.Error.Message });
            if (result.Error.Code.Contains("RateLimited")) return StatusCode(429, new { result.Error.Code, result.Error.Message });
            if (result.Error.Code.Contains("Forbidden")) return Forbid();
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Recebe um batch de até 100 logs de erro.
    /// Header obrigatório: X-Log-Api-Key.
    /// </summary>
    [HttpPost("ingest/batch")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<IngestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [RequestSizeLimit(3 * 1024 * 1024)] // 3 MB para batch de 100
    public async Task<IActionResult> IngestBatch(
        [FromHeader(Name = "X-Log-Api-Key")] string? apiKey,
        [FromBody] BatchIngestErrorLogRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { code = "ErrorLog.Unauthorized", message = "Header X-Log-Api-Key obrigatório." });

        var result = await _service.IngestBatchAsync(apiKey, request, ct);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("Unauthorized")) return Unauthorized(new { result.Error.Code, result.Error.Message });
            if (result.Error.Code.Contains("RateLimited")) return StatusCode(429, new { result.Error.Code, result.Error.Message });
        }

        return HandleResult(result);
    }

    // ===== Leitura (JWT obrigatório) =====

    /// <summary>
    /// Lista logs de erro com filtros e paginação cursor-based.
    /// </summary>
    [HttpGet]
    [Authorize]
    [RequirePermission("error-logs.view")]
    [ProducesResponseType(typeof(ErrorLogPagedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] string? appName,
        [FromQuery] string? level,
        [FromQuery] bool? isResolved,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var result = await _service.GetFilteredAsync(
            appName, level, isResolved, from, to, search, cursor, limit, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Estatísticas agregadas: total hoje, críticos, não resolvidos, por app.
    /// </summary>
    [HttpGet("aggregate/stats")]
    [Authorize]
    [RequirePermission("error-logs.view")]
    [ProducesResponseType(typeof(ErrorLogStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var result = await _service.GetStatsAsync(ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Detalhe completo de um log de erro.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [RequirePermission("error-logs.view")]
    [ProducesResponseType(typeof(ErrorLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Marca um log como resolvido com nota opcional.
    /// </summary>
    [HttpPut("{id:guid}/resolve")]
    [Authorize]
    [RequirePermission("error-logs.manage")]
    [ProducesResponseType(typeof(ErrorLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveErrorLogRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.ResolveAsync(id, request.Note, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove logs mais antigos que o período informado (mínimo 30 dias).
    /// </summary>
    [HttpDelete("cleanup")]
    [Authorize]
    [RequirePermission("error-logs.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cleanup(
        [FromQuery] int olderThanDays = 90,
        CancellationToken ct = default)
    {
        _logger.LogWarning("Cleanup de error logs solicitado: logs anteriores a {Days} dias", olderThanDays);
        var result = await _service.CleanupAsync(olderThanDays, ct);

        if (!result.IsSuccess) return HandleResult(result);

        return Ok(new
        {
            deletedCount = result.Value,
            message = $"{result.Value} logs removidos (anteriores a {olderThanDays} dias)"
        });
    }
}
