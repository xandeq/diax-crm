using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[Route("api/v1/finance/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly FinancialSummaryService _service;

    public SummaryController(FinancialSummaryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<FinancialSummaryResponse>> GetSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var request = new FinancialSummaryRequest
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _service.GetSummaryAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
