using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.OutreachAbTest;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiOutreachAbTestController : BaseAiController
{
    private readonly IOutreachAbTestService _service;

    public AiOutreachAbTestController(
        IOutreachAbTestService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiOutreachAbTestController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("outreach-ab-test")]
    public async Task<IActionResult> GenerateVariations([FromBody] GenerateAbVariationsRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/outreach-ab-test - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.GenerateVariationsAsync(request, userId, ct);
                return Ok(result);
            }
        );
    }
}
