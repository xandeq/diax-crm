using Asp.Versioning;
using Diax.Application.AI.Interfaces;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/ai-insights")]
[Authorize(Roles = "Admin")]
public class AiInsightsController : BaseApiController
{
    private readonly IAiInsightsService _aiInsightsService;
    private readonly DiaxDbContext _db;
    private readonly ILogger<AiInsightsController> _logger;

    public AiInsightsController(
        IAiInsightsService aiInsightsService,
        DiaxDbContext db,
        ILogger<AiInsightsController> logger)
    {
        _aiInsightsService = aiInsightsService;
        _db = db;
        _logger = logger;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyInsights(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var insights = await _aiInsightsService.GetDailyInsightsAsync(userId.Value, cancellationToken);
            return Ok(new { text = insights });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI insights");
            return StatusCode(500, new { Message = "Could not generate insights" });
        }
    }
}
