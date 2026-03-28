using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.Logs;
using Diax.Application.Logs.Dtos;
using Diax.Domain.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainLogLevel = Diax.Domain.Logs.LogLevel;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
[RequirePermission("logs.view")]
public class LogsController : BaseApiController
{
    private readonly AppLogService _service;
    private readonly ILogger<LogsController> _logger;

    public LogsController(AppLogService service, ILogger<LogsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] DomainLogLevel? level,
        [FromQuery] LogCategory? category,
        [FromQuery] string? search,
        [FromQuery] string? userId,
        [FromQuery] string? correlationId,
        [FromQuery] string? requestId,
        [FromQuery] string? path,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var request = new AppLogFilterRequest(
            fromDate, toDate, level, category, search,
            userId, correlationId, requestId, path, page, pageSize);

        var result = await _service.GetFilteredAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to retrieve logs: {Error}", result.Error?.Message);
            return StatusCode(500, result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Code == "Logs.NotFound")
                return NotFound(result.Error);
            return StatusCode(500, result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetStatsAsync(fromDate, toDate, cancellationToken);

        if (!result.IsSuccess)
            return StatusCode(500, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> Cleanup(
        [FromQuery] int olderThanDays = 90,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CleanupAsync(olderThanDays, cancellationToken);

        if (!result.IsSuccess)
            return StatusCode(500, result.Error);

        return Ok(new { DeletedCount = result.Value });
    }

    [HttpDelete("delete-all")]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAllAsync(cancellationToken);

        if (!result.IsSuccess)
            return StatusCode(500, result.Error);

        _logger.LogWarning("All logs deleted by user. Count: {Count}", result.Value);
        return Ok(new { DeletedCount = result.Value });
    }
}
