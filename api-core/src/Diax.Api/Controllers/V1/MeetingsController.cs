using Asp.Versioning;
using Diax.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Agendamento de reuniões: disponibilidade e reserva pelo link público (/agendar)
/// e gestão autenticada (listar/cancelar).
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/meetings")]
[Produces("application/json")]
public class MeetingsController : BaseApiController
{
    private readonly MeetingService _meetingService;

    public MeetingsController(MeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    /// <summary>Identificador da agenda do usuário logado (para montar o link público /agendar?u=...).</summary>
    [HttpGet("booking-link")]
    public IActionResult GetBookingLink()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return Ok(new { userId = userId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> ListUpcoming(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return Ok(await _meetingService.ListUpcomingAsync(userId.Value, ct));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _meetingService.CancelAsync(id, userId.Value, ct));
    }

    /// <summary>Slots livres dos próximos dias (link público de agendamento).</summary>
    [AllowAnonymous]
    [HttpGet("public/availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid u, [FromQuery] int days = 7, CancellationToken ct = default)
        => HandleResult(await _meetingService.GetPublicAvailabilityAsync(u, days, ct));

    /// <summary>Reserva de um slot pelo visitante (link público).</summary>
    [AllowAnonymous]
    [HttpPost("public/book")]
    public async Task<IActionResult> Book([FromBody] PublicBookingRequest request, CancellationToken ct)
        => HandleResult(await _meetingService.BookPublicAsync(request, ct));
}
