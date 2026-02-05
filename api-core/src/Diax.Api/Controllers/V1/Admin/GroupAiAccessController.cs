using Asp.Versioning;
using Diax.Application.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/groups/{groupId:guid}/ai-access")]
// [Authorize(Roles = "Admin")]
public class GroupAiAccessController : ControllerBase
{
    private readonly IGroupAiAccessService _service;

    public GroupAiAccessController(IGroupAiAccessService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<GroupAiAccessDto>> GetAccess(Guid groupId)
    {
        return Ok(await _service.GetGroupAccessAsync(groupId));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAccess(Guid groupId, [FromBody] UpdateGroupAiAccessRequest request)
    {
        await _service.UpdateGroupAccessAsync(groupId, request);
        return NoContent();
    }
}
