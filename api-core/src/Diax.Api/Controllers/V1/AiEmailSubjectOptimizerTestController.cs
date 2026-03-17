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
public class AiEmailSubjectOptimizerTestController : BaseAiController
{
    private readonly IEmailSubjectOptimizerService _service;

    public AiEmailSubjectOptimizerTestController(
        IEmailSubjectOptimizerService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiEmailSubjectOptimizerTestController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("test-email-subject")]
    public async Task<IActionResult> TestEndpoint([FromBody] GenerateSubjectLinesRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/test-email-subject - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return Ok(new { Message = "Test endpoint working" });
    }
}
