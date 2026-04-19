using Diax.Application.Calendar;
using Diax.Application.Calendar.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/appointment-labels")]
[Authorize]
public class AppointmentLabelsController : ControllerBase
{
    private readonly IAppointmentLabelService _labelService;

    public AppointmentLabelsController(IAppointmentLabelService labelService)
    {
        _labelService = labelService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _labelService.GetAllAsync(cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentLabelDto request, CancellationToken cancellationToken)
    {
        var result = await _labelService.CreateAsync(request, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateAppointmentLabelDto request, CancellationToken cancellationToken)
    {
        var result = await _labelService.UpdateAsync(id, request, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _labelService.DeleteAsync(id, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return NoContent();
    }
}
