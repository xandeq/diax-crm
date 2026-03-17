using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.EmailOptimization;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiEmailSubjectOptimizerController : BaseAiController
{
    private readonly IEmailSubjectOptimizerService _service;

    public AiEmailSubjectOptimizerController(
        IEmailSubjectOptimizerService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiEmailSubjectOptimizerController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("email-subject-optimizer")]
    public async Task<IActionResult> GenerateSubjectLines([FromBody] GenerateSubjectLinesRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/email-subject-optimizer - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.GenerateSubjectLinesAsync(request, userId, ct);
                return Ok(result);
            }
        );
    }
}
