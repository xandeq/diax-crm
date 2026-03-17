using Asp.Versioning;
using Diax.Application.Ai.HumanizeText;
using Diax.Application.AI;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiHumanizeTextController : BaseAiController
{
    private readonly IHumanizeTextService _service;

    public AiHumanizeTextController(
        IHumanizeTextService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiHumanizeTextController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("humanize-text")]
    public async Task<IActionResult> Humanize([FromBody] HumanizeTextRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/humanize-text - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.HumanizeAsync(request, userId);
                return Ok(result);
            }
        );
    }
}
