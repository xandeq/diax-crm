using Asp.Versioning;
using Diax.Application.AI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/ai-insights")]
[Authorize(Roles = "Admin")]
public class AiInsightsController : ControllerBase
{
    private readonly IAiInsightsService _aiInsightsService;
    private readonly ILogger<AiInsightsController> _logger;

    public AiInsightsController(IAiInsightsService aiInsightsService, ILogger<AiInsightsController> logger)
    {
        _aiInsightsService = aiInsightsService;
        _logger = logger;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyInsights(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var insights = await _aiInsightsService.GetDailyInsightsAsync(userId, cancellationToken);
            return Ok(new { text = insights });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI insights");
            return StatusCode(500, new { Message = "Could not generate insights" });
        }
    }
}
