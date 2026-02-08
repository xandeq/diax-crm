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

    [HttpPost("{providerId}/batch-models")]
    public async Task<IActionResult> AddModelsBatch(Guid providerId, [FromBody] List<DiscoveredModelDto> models, CancellationToken cancellationToken)
    {
        try
        {
            await _providerService.UpdateModelsBatchAsync(providerId, models, cancellationToken);
            return Ok(new { success = true, message = $"{models.Count} modelos processados com sucesso." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Provider not found");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao processar modelos", detail = ex.Message });
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

    /// <summary>
    /// Discover available models from provider's external API (OpenRouter only for now)
    /// </summary>
    [HttpGet("discover-models/{providerKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DiscoverModels(string providerKey, CancellationToken cancellationToken)
    {
        try
        {
            var models = await _providerService.DiscoverModelsAsync(providerKey, cancellationToken);
            return Ok(new { success = true, data = models, totalCount = models.Count() });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { success = false, error = "Não foi possível conectar ao provedor", details = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Erro ao descobrir modelos", details = ex.Message });
        }
    }
}
