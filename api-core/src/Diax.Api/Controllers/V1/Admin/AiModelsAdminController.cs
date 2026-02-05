using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/ai/models")]
[Authorize(Roles = "Admin")]
public class AiModelsAdminController : ControllerBase
{
    private readonly IAiProviderAdminService _providerService;

    public AiModelsAdminController(IAiProviderAdminService providerService)
    {
        _providerService = providerService;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AiModelDto modelDto, CancellationToken cancellationToken)
    {
        try
        {
            await _providerService.UpdateModelAsync(id, modelDto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _providerService.DeleteModelAsync(id, cancellationToken);
        return NoContent();
    }
}
