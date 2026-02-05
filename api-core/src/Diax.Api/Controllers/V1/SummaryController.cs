using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/finance/[controller]")]
public class SummaryController : BaseApiController
{
    private readonly FinancialSummaryService _service;
    private readonly DiaxDbContext _db;

    public SummaryController(FinancialSummaryService service, DiaxDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
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
