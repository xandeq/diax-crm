using Asp.Versioning;
using Diax.Api.Controllers.V1.Common;
using Diax.Application.Ai.Dtos;
using Diax.Application.Ai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai-usage")]
[Produces("application/json")]
public class AiUsageController : BaseApiController
{
    private readonly IAiUsageLogService _service;
    private readonly ILogger<AiUsageController> _logger;

    public AiUsageController(
        IAiUsageLogService service,
        ILogger<AiUsageController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Retorna sumário de uso de IA com filtros opcionais
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AiUsageSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? providerId,
        [FromQuery] Guid? modelId,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var request = new AiUsageSummaryRequestDto(
            StartDate: startDate,
            EndDate: endDate,
            ProviderId: providerId,
            ModelId: modelId,
            UserId: userId
        );

        _logger.LogInformation(
            "AI Usage Summary requested with filters: StartDate={StartDate}, EndDate={EndDate}",
            startDate, endDate);

        var result = await _service.GetSummaryAsync(request, cancellationToken);

        return HandleResult(result);
    }
}
