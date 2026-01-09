using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class CreditCardGroupsController : ControllerBase
{
    private readonly CreditCardGroupService _service;

    public CreditCardGroupsController(CreditCardGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CreditCardGroupResponse>>> GetAll()
    {
        var groups = await _service.GetAllAsync();
        return Ok(groups);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CreditCardGroupResponse>>> GetActive()
    {
        var groups = await _service.GetActiveGroupsAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditCardGroupResponse>> GetById(Guid id)
    {
        var group = await _service.GetByIdAsync(id);
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardGroupResponse>> Create(CreateCreditCardGroupRequest request)
    {
        var group = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CreditCardGroupResponse>> Update(Guid id, UpdateCreditCardGroupRequest request)
    {
        var group = await _service.UpdateAsync(id, request);
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
