using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.EmailMarketing.Pro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[RequirePermission("campaigns.manage")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leads/normalization")]
[Produces("application/json")]
public class LeadNormalizationController : BaseApiController
{
    private readonly ILeadNormalizationService _service;

    public LeadNormalizationController(ILeadNormalizationService service)
    {
        _service = service;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _service.GetStatsAsync(ct);
        return Ok(result);
    }

    [HttpGet("preview")]
    public async Task<IActionResult> GetPreview(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetPreviewAsync(page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunBatch(
        [FromQuery] bool forceReprocess = false,
        CancellationToken ct = default)
    {
        var result = await _service.RunBatchAsync(forceReprocess, ct);
        return Ok(result);
    }
}
