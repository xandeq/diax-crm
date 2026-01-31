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
[Produces("application/json")]
[Authorize]
public class PromptGeneratorController : BaseApiController
{
    private readonly IPromptGeneratorService _service;
    private readonly ILogger<PromptGeneratorController> _logger;

    public PromptGeneratorController(
        IPromptGeneratorService service,
        ILogger<PromptGeneratorController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("models")]
    public IActionResult GetModels()
    {
        return Ok(AiModelCatalog.Providers);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GeneratePromptRequestDto request)
    {
        _logger.LogInformation("POST /api/v1/prompt-generator/generate - Request received");

        if (request is null || string.IsNullOrWhiteSpace(request.RawPrompt))
        {
            return BadRequest(new
            {
                Code = "PromptGenerator.Validation",
                Message = "O prompt não pode ser vazio."
            });
        }

        var provider = string.IsNullOrWhiteSpace(request.Provider) ? "chatgpt" : request.Provider;
        var promptType = string.IsNullOrWhiteSpace(request.PromptType) ? "professional" : request.PromptType;

        try
        {
            var finalPrompt = await _service.GenerateAsync(request.RawPrompt, provider, promptType, request.Model);
            return Ok(new GeneratePromptResponseDto(finalPrompt));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Prompt generation failed for provider {Provider}", provider);
            return StatusCode(500, new
            {
                Code = "PromptGenerator.Failed",
                Message = "Falha ao gerar prompt. Tente novamente."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating prompt for provider {Provider}", provider);
            return StatusCode(500, new
            {
                Code = "PromptGenerator.Unexpected",
                Message = "Erro inesperado ao gerar prompt."
            });
        }
    }
}
