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
    private readonly IVideoJobService _videoJobService;

    public AiImageGenerationController(
        IImageGenerationService service,
        IVideoGenerationService videoService,
        IVideoJobService videoJobService,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiImageGenerationController> logger)
        : base(catalogService, db, logger)
    {
        _service = service;
        _videoService = videoService;
        _videoJobService = videoJobService;
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

    /// <summary>
    /// Enfileira uma geração de vídeo assíncrona (recomendado — vídeos podem levar minutos).
    /// Retorna 202 com o jobId; consulte o status em GET video-jobs/{id}.
    /// </summary>
    [HttpPost("video-jobs")]
    public async Task<IActionResult> CreateVideoJob([FromBody] VideoGenerationRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("POST /api/v1/ai/video-jobs - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        return await ExecuteAiActionAsync(
            request.Provider,
            request.Model,
            ct,
            async userId => {
                var job = await _videoJobService.EnqueueAsync(request, userId, ct);
                return StatusCode(202, job);
            }
        );
    }

    /// <summary>Status de um job de geração de vídeo (só do próprio usuário).</summary>
    [HttpGet("video-jobs/{jobId:guid}")]
    public async Task<IActionResult> GetVideoJob([FromRoute] Guid jobId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Usuário não autenticado." });

        var job = await _videoJobService.GetAsync(jobId, userId.Value, ct);
        if (job == null)
            return NotFound(new { Message = "Job não encontrado." });

        return Ok(job);
    }

    /// <summary>Lista os jobs de vídeo do usuário (mais recentes primeiro).</summary>
    [HttpGet("video-jobs")]
    public async Task<IActionResult> ListVideoJobs([FromQuery] int take, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Usuário não autenticado." });

        var jobs = await _videoJobService.ListAsync(userId.Value, take <= 0 ? 20 : take, ct);
        return Ok(jobs);
    }
}
