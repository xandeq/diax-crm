using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.SocialMediaBatch;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiSocialMediaBatchController : BaseAiController
{
    private readonly ISocialMediaBatchService _service;

    public AiSocialMediaBatchController(
        ISocialMediaBatchService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiSocialMediaBatchController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
    }

    [HttpPost("social-media-batch")]
    public async Task<IActionResult> GenerateBatch([FromBody] GenerateSocialBatchRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/social-media-batch - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.GenerateBatchAsync(request, userId, ct);
                return Ok(result);
            }
        );
    }
}
