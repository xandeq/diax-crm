using Asp.Versioning;
using Diax.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>Dashboard comercial: funil, receita mensal, propostas e compromissos.</summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sales-dashboard")]
[Produces("application/json")]
public class SalesDashboardController : BaseApiController
{
    private readonly SalesDashboardService _dashboardService;

    public SalesDashboardController(SalesDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return Ok(await _dashboardService.GetAsync(userId.Value, ct));
    }
}
