using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.ImageGeneration;
using Diax.Application.AI.ImageGeneration.Dtos;
using Diax.Application.AI.VideoGeneration;
using Diax.Application.AI.VideoGeneration.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
[Authorize]
public class AiImageGenerationController : BaseApiController
{
    private readonly IImageGenerationService _service;
    private readonly IVideoGenerationService _videoService;
    private readonly IAiCatalogService _catalogService;
    private readonly DiaxDbContext _db;
    private readonly ILogger<AiImageGenerationController> _logger;

    public AiImageGenerationController(
        IImageGenerationService service,
        IVideoGenerationService videoService,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiImageGenerationController> logger)
    {
        _service = service;
        _videoService = videoService;
        _catalogService = catalogService;
        _db = db;
        _logger = logger;
    }

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] ImageGenerationRequestDto request)
    {
        _logger.LogInformation("POST /api/v1/ai/generate-image - Request received");

        if (request is null)
        {
            return BadRequest(new { Message = "Payload inválido." });
        }

        try
        {
            var userId = await ResolveUserIdAsync(_db, CancellationToken.None);
            if (userId == null)
            {
                return Unauthorized(new { Message = "User not authenticated." });
            }

            // Validate RBAC access
            var hasAccess = await _catalogService.ValidateUserAccessAsync(
                userId.Value,
                request.Provider,
                request.Model,
                CancellationToken.None);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to use unauthorized provider/model for image generation: {Provider}/{Model}",
                    userId, request.Provider, request.Model);

                return StatusCode(403, new
                {
                    Message = "Você não tem permissão para usar este provider/modelo de IA. Contate o administrador."
                });
            }

            var result = await _service.GenerateAsync(request, userId.Value);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AiImageGenerationController.");
            return StatusCode(500, new { Message = "Erro inesperado ao gerar imagem." });
        }
    }

    [HttpPost("generate-video")]
    public async Task<IActionResult> GenerateVideo([FromBody] VideoGenerationRequestDto request)
    {
        _logger.LogInformation("POST /api/v1/ai/generate-video - Request received");

        if (request is null)
            return BadRequest(new { Message = "Payload inválido." });

        try
        {
            var userId = await ResolveUserIdAsync(_db, CancellationToken.None);
            if (userId == null)
                return Unauthorized(new { Message = "User not authenticated." });

            var hasAccess = await _catalogService.ValidateUserAccessAsync(
                userId.Value,
                request.Provider,
                request.Model,
                CancellationToken.None);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to use unauthorized provider/model for video generation: {Provider}/{Model}",
                    userId, request.Provider, request.Model);
                return StatusCode(403, new
                {
                    Message = "Você não tem permissão para usar este provider/modelo de IA. Contate o administrador."
                });
            }

            var result = await _videoService.GenerateAsync(request, userId.Value);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { Message = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(504, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AiImageGenerationController.GenerateVideo.");
            return StatusCode(500, new { Message = "Erro inesperado ao gerar vídeo." });
        }
    }
}
