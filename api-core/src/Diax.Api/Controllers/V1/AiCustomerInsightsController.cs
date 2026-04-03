using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.CustomerInsights;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiCustomerInsightsController : BaseAiController
{
    private readonly ICustomerInsightsService _service;

    public AiCustomerInsightsController(
        ICustomerInsightsService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiCustomerInsightsController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("customer-insights")]
    public async Task<IActionResult> GenerateInsights([FromBody] GenerateInsightsRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/customer-insights - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.GenerateInsightsAsync(request, userId, ct);
                return Ok(result);
            }
        );
    }
}
