using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CreditCardGroupsController : BaseApiController
{
    private readonly CreditCardGroupService _service;
    private readonly DiaxDbContext _db;

    public CreditCardGroupsController(CreditCardGroupService service, DiaxDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CreditCardGroupResponse>>> GetAll(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var groups = await _service.GetAllAsync(userId.Value);
        return Ok(groups);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CreditCardGroupResponse>>> GetActive(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var groups = await _service.GetActiveGroupsAsync(userId.Value);
        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditCardGroupResponse>> GetById(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var group = await _service.GetByIdAsync(id, userId.Value);
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardGroupResponse>> Create(CreateCreditCardGroupRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var group = await _service.CreateAsync(request, userId.Value);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CreditCardGroupResponse>> Update(Guid id, UpdateCreditCardGroupRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var group = await _service.UpdateAsync(id, request, userId.Value);
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(id, userId.Value);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
