using Asp.Versioning;
using Diax.Application.HtmlExtraction;
using Diax.Application.HtmlExtraction.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class HtmlExtractionController : BaseApiController
{
    private readonly HtmlExtractionService _service;
    private readonly ILogger<HtmlExtractionController> _logger;

    public HtmlExtractionController(HtmlExtractionService service, ILogger<HtmlExtractionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("extract-text")]
    public async Task<IActionResult> ExtractText([FromBody] ExtractTextRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/htmlextraction/extract-text - Request received");

        var result = await _service.ExtractTextAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("POST /api/v1/htmlextraction/extract-text - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }

        _logger.LogInformation("POST /api/v1/htmlextraction/extract-text - Success");
        return Ok(result.Value);
    }

    [HttpPost("extract-urls")]
    public async Task<IActionResult> ExtractUrls([FromBody] ExtractUrlsRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/htmlextraction/extract-urls - Request received");

        var result = await _service.ExtractUrlsAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("POST /api/v1/htmlextraction/extract-urls - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }

        _logger.LogInformation("POST /api/v1/htmlextraction/extract-urls - Success");
        return Ok(result.Value);
    }
}
