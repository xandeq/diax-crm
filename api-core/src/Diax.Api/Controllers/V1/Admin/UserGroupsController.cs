using Asp.Versioning;
using Diax.Application.Auth;
using Diax.Application.Auth.Dtos;
using Diax.Domain.UserGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/groups")]
// [Authorize(Roles = "Admin")]
public class UserGroupsController : ControllerBase
{
    private readonly IUserGroupService _userGroupService;

    public UserGroupsController(IUserGroupService userGroupService)
    {
        _userGroupService = userGroupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserGroupDto>>> GetAll()
    {
        return Ok(await _userGroupService.GetAllWithCountAsync());
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

    // ── Members management ──

    [HttpGet("{groupId:guid}/members")]
    public async Task<ActionResult<List<GroupMemberDto>>> GetMembers(Guid groupId)
    {
        try
        {
            var members = await _userGroupService.GetMembersAsync(groupId);
            return Ok(members);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddMemberRequest request)
    {
        try
        {
            await _userGroupService.AddMemberAsync(groupId, request.UserId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
    {
        try
        {
            await _userGroupService.RemoveMemberAsync(groupId, userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public record CreateUserGroupRequest(string Name, string Description);
public record UpdateUserGroupRequest(string Name, string Description);
public record AddMemberRequest(Guid UserId);
