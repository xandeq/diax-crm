using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/finance/[controller]")]
public class SummaryController : BaseApiController
{
    private readonly FinancialSummaryService _service;

    public SummaryController(FinancialSummaryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var request = new FinancialSummaryRequest
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _service.GetSummaryAsync(request, userId.Value, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
