using Asp.Versioning;
using Diax.Application.Auth;
using Diax.Domain.UserGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/groups")]
// [Authorize(Roles = "Admin")] // Uncomment when Auth is fully active
public class UserGroupsController : ControllerBase
{
    private readonly IUserGroupService _userGroupService;

    public UserGroupsController(IUserGroupService userGroupService)
    {
        _userGroupService = userGroupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserGroup>>> GetAll()
    {
        return Ok(await _userGroupService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserGroup>> GetById(Guid id)
    {
        var group = await _userGroupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        return Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<UserGroup>> Create([FromBody] CreateUserGroupRequest request)
    {
        var group = await _userGroupService.CreateAsync(request.Name, request.Description);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserGroupRequest request)
    {
        try
        {
            await _userGroupService.UpdateAsync(id, request.Name, request.Description);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _userGroupService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Members endpoints omitted for brevity for now, focus on Group management first
}

public record CreateUserGroupRequest(string Name, string Description);
public record UpdateUserGroupRequest(string Name, string Description);
