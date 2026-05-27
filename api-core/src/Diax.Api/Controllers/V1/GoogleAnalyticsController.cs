using Asp.Versioning;
using Diax.Application.GoogleAnalytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller para Google Analytics 4 — relatórios via GA4 Data API.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class GoogleAnalyticsController : BaseApiController
{
    private readonly IGoogleAnalyticsService _ga4;

    public GoogleAnalyticsController(IGoogleAnalyticsService ga4)
    {
        _ga4 = ga4;
    }

    /// <summary>
    /// Retorna status de configuração do GA4.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(_ga4.GetStatus());

    /// <summary>
    /// Relatório consolidado: overview, tráfego, páginas, dispositivos, série temporal.
    /// </summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        if (days < 1 || days > 365)
            return BadRequest("days deve ser entre 1 e 365.");

        return HandleResult(await _ga4.GetReportAsync(days, ct));
    }
}
