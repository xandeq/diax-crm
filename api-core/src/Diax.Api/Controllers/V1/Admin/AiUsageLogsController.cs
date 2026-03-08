using Asp.Versioning;
using Diax.Domain.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/ai/usage-logs")]
[Authorize(Roles = "Admin")]
public class AiUsageLogsController : ControllerBase
{
    private readonly IAiUsageLogRepository _repository;
    private readonly ILogger<AiUsageLogsController> _logger;

    public AiUsageLogsController(
        IAiUsageLogRepository repository,
        ILogger<AiUsageLogsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get AI usage logs with optional filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? providerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<AiUsageLog> logs;

            if (startDate.HasValue && endDate.HasValue)
            {
                logs = await _repository.GetByDateRangeAsync(startDate.Value, endDate.Value, skip, take, cancellationToken);
            }
            else if (userId.HasValue)
            {
                logs = await _repository.GetByUserIdAsync(userId.Value, skip, take, cancellationToken);
            }
            else if (providerId.HasValue)
            {
                logs = await _repository.GetByProviderIdAsync(providerId.Value, skip, take, cancellationToken);
            }
            else
            {
                // Default: recent logs by date
                var defaultEndDate = DateTime.UtcNow;
                var defaultStartDate = defaultEndDate.AddDays(-7);
                logs = await _repository.GetByDateRangeAsync(defaultStartDate, defaultEndDate, skip, take, cancellationToken);
            }

            var result = logs.Select(log => new
            {
                log.Id,
                log.UserId,
                log.ProviderId,
                ProviderName = log.Provider?.Name,
                ProviderKey = log.Provider?.Key,
                log.ModelId,
                ModelName = log.Model?.DisplayName,
                ModelKey = log.Model?.ModelKey,
                log.FeatureType,
                log.InputTokens,
                log.OutputTokens,
                TotalTokens = (log.InputTokens ?? 0) + (log.OutputTokens ?? 0),
                log.EstimatedCost,
                log.Duration,
                DurationMs = log.Duration.TotalMilliseconds,
                log.Success,
                log.ErrorMessage,
                log.RequestId,
                log.CreatedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching AI usage logs");
            return StatusCode(500, new { Message = "Error fetching usage logs", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Get aggregated usage statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? providerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _repository.GetUsageStatsAsync(
                userId,
                providerId,
                startDate,
                endDate,
                cancellationToken
            );

            return Ok(new
            {
                TotalRequests = stats.totalLogs,
                TotalTokens = stats.totalTokens,
                TotalCost = stats.totalCost,
                Period = new
                {
                    StartDate = startDate,
                    EndDate = endDate
                },
                Filters = new
                {
                    UserId = userId,
                    ProviderId = providerId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching AI usage stats");
            return StatusCode(500, new { Message = "Error fetching usage statistics", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Get aggregated usage statistics grouped by provider
    /// </summary>
    [HttpGet("stats/grouped")]
    public async Task<IActionResult> GetGroupedStats(
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _repository.GetGroupedByProviderStatsAsync(
                userId,
                startDate,
                endDate,
                cancellationToken
            );

            var result = stats.Select(s => new
            {
                ProviderId = s.ProviderId,
                ProviderName = s.ProviderName,
                TotalRequests = s.TotalLogs,
                TotalTokens = s.TotalTokens,
                TotalCost = s.TotalCost
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching grouped AI usage stats");
            return StatusCode(500, new { Message = "Error fetching grouped usage statistics", Detail = ex.Message });
        }
    }
}
