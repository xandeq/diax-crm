using Asp.Versioning;
using Diax.Application.Briefings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Área "Daily Briefings" do CRM — lista e leitura dos briefings do dia corrente.
/// A ingestão (geradores → CRM) fica no IntegrationsController (auth por X-Integration-Key).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/daily-briefings")]
[Produces("application/json")]
[Authorize]
public class DailyBriefingsController : BaseApiController
{
    private readonly IDailyBriefingService _service;

    public DailyBriefingsController(IDailyBriefingService service)
    {
        _service = service;
    }

    /// <summary>Cards dos briefings do dia corrente.</summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _service.GetTodayAsync(userId.Value, ct);
        return HandleResult(result);
    }

    /// <summary>Conteúdo completo de um briefing.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _service.GetByIdAsync(userId.Value, id, ct);
        return HandleResult(result);
    }
}
