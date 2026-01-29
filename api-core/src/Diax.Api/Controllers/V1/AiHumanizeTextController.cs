using Asp.Versioning;
using Diax.Application.Ai.HumanizeText;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Produces("application/json")]
[Authorize]
public class AiHumanizeTextController : BaseApiController
{
    private readonly IHumanizeTextService _service;
    private readonly ILogger<AiHumanizeTextController> _logger;

    public AiHumanizeTextController(
        IHumanizeTextService service,
        ILogger<AiHumanizeTextController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("humanize-text")]
    public async Task<IActionResult> Humanize([FromBody] HumanizeTextRequestDto request)
    {
        _logger.LogInformation("POST /api/v1/ai/humanize-text - Request received");

        if (request is null)
        {
            return BadRequest(new { Message = "Payload inválido." });
        }

        try
        {
            var result = await _service.HumanizeAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Geralmente erros de configuração ou do provedor tratados amigavelmente
            return StatusCode(502, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in HumanizeTextController.");
            return StatusCode(500, new { Message = "Erro inesperado ao processar sua solicitação." });
        }
    }
}
