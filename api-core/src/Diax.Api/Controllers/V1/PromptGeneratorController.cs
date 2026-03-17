using Asp.Versioning;
using Diax.Application.PromptGenerator;
using Diax.Application.PromptGenerator.Dtos;
using Diax.Application.AI;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prompt-generator")]
public class PromptGeneratorController : BaseApiController
{
    private readonly IPromptGeneratorService _promptGeneratorService;
    private readonly ILogger<PromptGeneratorController> _logger;
    private readonly DiaxDbContext _db;
    private readonly IAiCatalogService _aiCatalogService;

    public PromptGeneratorController(
        IPromptGeneratorService promptGeneratorService,
        ILogger<PromptGeneratorController> logger,
        DiaxDbContext db,
        IAiCatalogService aiCatalogService)
    {
        _promptGeneratorService = promptGeneratorService;
        _logger = logger;
        _db = db;
        _aiCatalogService = aiCatalogService;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GeneratePromptResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PromptErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PromptErrorResponseDto), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(PromptErrorResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Generate([FromBody] GeneratePromptRequestDto request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(request.RawPrompt))
        {
            return BadRequest(new PromptErrorResponseDto
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                Message = "O prompt não pode estar vazio.",
                CorrelationId = correlationId
            });
        }

        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (userId == null)
            return Unauthorized(new { error = "Usuário não autenticado." });

        // Validate RBAC Access to Provider/Model
        if (request.Provider != null && request.Model != null)
        {
            var hasAccess = await _aiCatalogService.ValidateUserAccessAsync(userId.Value, request.Provider, request.Model, cancellationToken);
            if (!hasAccess)
            {
                _logger.LogWarning("Access denied for User {UserId} to Provider {Provider} Model {Model}", userId, request.Provider, request.Model);
                return StatusCode(403, new { error = "Você não tem permissão para usar este provedor ou modelo de IA." });
            }
        }

        try
        {
            _logger.LogInformation(
                "Generate request started. User: {UserId}. Provider: {Provider}. Model: {Model}. PromptType: {PromptType}. CorrelationId: {CorrelationId}",
                userId,
                request.Provider ?? "default",
                request.Model ?? "default",
                request.PromptType ?? "default",
                correlationId);

            var result = await _promptGeneratorService.GenerateAndSaveAsync(
                request.RawPrompt,
                request.Provider ?? "chatgpt",
                request.PromptType ?? "professional",
                request.Model,
                userId.Value);

            return Ok(new GeneratePromptResponseDto(result));
        }
        catch (PromptGeneratorException ex)
        {
            _logger.LogWarning(ex,
                "Provider error. Provider: {Provider}. StatusCode: {StatusCode}. Message: {Message}. CorrelationId: {CorrelationId}",
                ex.Provider, ex.StatusCode, ex.ErrorMessage, correlationId);

            var httpStatus = ex.StatusCode switch
            {
                401 or 403 => StatusCodes.Status502BadGateway,
                429 => StatusCodes.Status429TooManyRequests,
                >= 500 => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(httpStatus, new PromptErrorResponseDto
            {
                Success = false,
                ErrorCode = $"PROVIDER_ERROR_{ex.StatusCode}",
                Message = ex.GetSafeMessage(),
                Provider = ex.Provider,
                CorrelationId = correlationId
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error. CorrelationId: {CorrelationId}", correlationId);

            return BadRequest(new PromptErrorResponseDto
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            _logger.LogError(ex, "Configuration error. CorrelationId: {CorrelationId}", correlationId);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new PromptErrorResponseDto
            {
                Success = false,
                ErrorCode = "CONFIGURATION_ERROR",
                Message = "Service temporarily unavailable. Please contact support.",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// Retorna o histórico de prompts do usuário logado.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<UserPromptHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (userId == null)
            return Unauthorized(new { error = "User not found" });

        var history = await _promptGeneratorService.GetUserHistoryAsync(userId.Value, limit, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Retorna os detalhes de um prompt específico do usuário.
    /// </summary>
    [HttpGet("history/{id:guid}")]
    [ProducesResponseType(typeof(UserPromptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPromptById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (userId == null)
            return Unauthorized(new { error = "User not found" });

        var prompt = await _promptGeneratorService.GetPromptByIdAsync(id, userId.Value, cancellationToken);

        if (prompt == null)
            return NotFound(new { error = "Prompt not found" });

        return Ok(prompt);
    }

    // ENDPOINT REMOVED: Use /api/v1/ai/catalog instead (filtered by group permissions)
    // [HttpGet("providers")]
    // [AllowAnonymous]
    // public async Task<IActionResult> GetProviders(...) { ... }

}
