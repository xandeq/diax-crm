using Asp.Versioning;
using Diax.Application.Helpdesk;
using Diax.Application.Helpdesk.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/helpdesk/tickets")]
[Produces("application/json")]
[Authorize]
public class TicketsController : BaseApiController
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TicketsQuery query, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.GetAllAsync(userId.Value, query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.GetByIdAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.CreateAsync(request, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.UpdateAsync(id, request, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.DeleteAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.ResolveAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.ReopenAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _ticketService.CloseAsync(id, userId.Value, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
