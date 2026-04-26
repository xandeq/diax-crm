using Asp.Versioning;
using Diax.Application.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/planner/investiq")]
[Produces("application/json")]
public class InvestIQController : BaseApiController
{
    private readonly IInvestIQIntegrationService _investIQ;

    public InvestIQController(IInvestIQIntegrationService investIQ)
    {
        _investIQ = investIQ;
    }

    /// <summary>
    /// Retorna o resumo do portfólio de investimentos do InvestIQ
    /// </summary>
    [HttpGet("portfolio-summary")]
    public async Task<IActionResult> GetPortfolioSummary(CancellationToken cancellationToken)
    {
        var result = await _investIQ.GetPortfolioSummaryAsync(cancellationToken);

        if (result.IsFailure && result.Error.Code == "InvestIQ.NotConfigured")
            return Ok(new { configured = false });

        return result.IsSuccess ? Ok(result.Value) : StatusCode(502, result.Error);
    }
}
