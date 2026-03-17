using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.LeadPersona;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiLeadPersonaGeneratorController : BaseAiController
{
    private readonly ILeadPersonaGeneratorService _service;

    public AiLeadPersonaGeneratorController(
        ILeadPersonaGeneratorService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiLeadPersonaGeneratorController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("lead-personas")]
    public async Task<IActionResult> GeneratePersonas([FromBody] GeneratePersonasRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/lead-personas - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.GeneratePersonasAsync(request, userId, ct);
                return Ok(result);
            }
        );
    }
}
