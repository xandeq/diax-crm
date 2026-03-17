using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.ImageGeneration;
using Diax.Application.AI.ImageGeneration.Dtos;
using Diax.Application.AI.VideoGeneration;
using Diax.Application.AI.VideoGeneration.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
public class AiImageGenerationController : BaseAiController
{
    private readonly IImageGenerationService _service;
    private readonly IVideoGenerationService _videoService;

    public AiImageGenerationController(
        IImageGenerationService service,
        IVideoGenerationService videoService,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiImageGenerationController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
        _videoService = videoService;
    }

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] ImageGenerationRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/generate-image - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _service.GenerateAsync(request, userId);
                return Ok(result);
            }
        );
    }

    [HttpPost("generate-video")]
    public async Task<IActionResult> GenerateVideo([FromBody] VideoGenerationRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/generate-video - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var result = await _videoService.GenerateAsync(request, userId);
                return Ok(result);
            },
            customExceptionHandler: ex => {
                if (ex is TimeoutException timeoutEx)
                    return StatusCode(504, new { Message = timeoutEx.Message });
                return null; // Fall through to standard handlers
            }
        );
    }
}
