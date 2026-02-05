using Asp.Versioning;
using Diax.Application.AI;
using Diax.Application.AI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/ai/providers")]
[Authorize(Roles = "Admin")] // Enforces Role=Admin
public class AiProvidersAdminController : ControllerBase
{
    private readonly IAiProviderAdminService _providerService;

    public AiProvidersAdminController(IAiProviderAdminService providerService)
    {
        _providerService = providerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _providerService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _providerService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAiProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _providerService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1" }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAiProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _providerService.UpdateAsync(id, request, cancellationToken);
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
        await _providerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // --- Models Sub-resource ---

    [HttpGet("{providerId}/models")]
    public async Task<IActionResult> GetModels(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _providerService.GetModelsByProviderIdAsync(providerId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{providerId}/models")]
    public async Task<IActionResult> AddModel(Guid providerId, [FromBody] AiModelDto modelDto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _providerService.AddModelAsync(providerId, modelDto, cancellationToken);
            return CreatedAtAction(nameof(GetModels), new { providerId, version = "1" }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Provider not found");
        }
    }

    [HttpPost("{providerId}/sync-models")]
    public async Task<IActionResult> SyncModels(Guid providerId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _providerService.SyncModelsAsync(providerId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
