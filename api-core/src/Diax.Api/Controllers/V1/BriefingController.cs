using Asp.Versioning;
using Diax.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>Briefing comercial (Telegram): preview e envio manual.</summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/briefing")]
[Produces("application/json")]
public class BriefingController : BaseApiController
{
    private readonly CommercialBriefingService _briefingService;

    public BriefingController(CommercialBriefingService briefingService)
    {
        _briefingService = briefingService;
    }

    /// <summary>Preview do briefing (não envia).</summary>
    [HttpGet("commercial/preview")]
    public async Task<IActionResult> Preview(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });

        var data = await _briefingService.ComposeAsync(userId.Value, ct);
        return Ok(new
        {
            text = CommercialBriefingService.FormatBriefing(data),
            meetings = data.MeetingsToday.Count,
            followUps = data.FollowUpsDue.Count,
            hotLeads = data.HotLeads.Count,
            pendingProposals = data.PendingProposals.Count,
        });
    }

    /// <summary>Envia o briefing agora para o Telegram configurado.</summary>
    [HttpPost("commercial/send")]
    public async Task<IActionResult> Send(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _briefingService.SendAsync(userId.Value, ct));
    }
}
