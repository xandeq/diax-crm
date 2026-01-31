using Asp.Versioning;
using Diax.Application.PromptGenerator;
using Diax.Application.PromptGenerator.Common;
using Diax.Application.PromptGenerator.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prompt-generator")]
[Authorize]
public class PromptGeneratorController : ControllerBase
{
    private readonly IPromptGeneratorService _promptGeneratorService;
    private readonly ILogger<PromptGeneratorController> _logger;

    public PromptGeneratorController(
        IPromptGeneratorService promptGeneratorService,
        ILogger<PromptGeneratorController> logger)
    {
        _promptGeneratorService = promptGeneratorService;
        _logger = logger;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GeneratePromptResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PromptErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PromptErrorResponseDto), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(PromptErrorResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Generate([FromBody] GeneratePromptRequestDto request)
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

        try
        {
            _logger.LogInformation(
                "Generate request started. Provider: {Provider}. Model: {Model}. PromptType: {PromptType}. CorrelationId: {CorrelationId}",
                request.Provider ?? "default",
                request.Model ?? "default",
                request.PromptType ?? "default",
                correlationId);

            var result = await _promptGeneratorService.GenerateAsync(
                request.RawPrompt,
                request.Provider ?? "chatgpt",
                request.PromptType ?? "professional",
                request.Model);

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
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error generating prompt. CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(StatusCodes.Status500InternalServerError, new PromptErrorResponseDto
            {
                Success = false,
                ErrorCode = "INTERNAL_ERROR",
                Message = "Falha ao gerar prompt. Tente novamente.",
                CorrelationId = correlationId
            });
        }
    }

    [HttpGet("providers")]
    [ProducesResponseType(typeof(List<ProviderModelsDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public IActionResult GetProviders()
    {
        return Ok(AiModelCatalog.Providers);
    }
}
