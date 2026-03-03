using Diax.Application.Calendar;
using Diax.Application.Calendar.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreateAsync(request, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("import-text")]
    public async Task<IActionResult> ImportText([FromBody] ImportTextRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.ParseFromTextAsync(request.Text, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure) return NotFound(result.Error);
        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppointmentDto request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.UpdateAsync(id, request, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.DeleteAsync(id, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return NoContent();
    }

    [AllowAnonymous] // Cron call is external without Bearer
    [HttpPost("trigger-daily-notification")]
    public async Task<IActionResult> TriggerDailyNotification([FromHeader(Name = "X-Cron-Key")] string cronKey, CancellationToken cancellationToken)
    {
        // Simple security check - should match env var CRON_SECURITY_KEY
        var expectedKey = Environment.GetEnvironmentVariable("CRON_SECURITY_KEY") ?? "diax-cron-dev-key";
        if (cronKey != expectedKey)
        {
            return Unauthorized("Invalid cron key.");
        }

        var result = await _appointmentService.SendDailyAgendaNotificationAsync(cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(new { message = "Daily notification triggered successfully." });
    }
}
