using Diax.Application.AI;
using Diax.Application.AI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Endpoints para gerenciamento dinâmico de providers e modelos de IA.
/// Permite inserir, ativar e desativar modelos sem alterar código.
/// </summary>
[ApiController]
[Route("api/v1/ai/management")]
[Authorize]
public class AiProviderManagementController : ControllerBase
{
    private readonly IAiProviderManagementService _managementService;

    public AiProviderManagementController(IAiProviderManagementService managementService)
    {
        _managementService = managementService;
    }

    /// <summary>
    /// Lista todos os providers e modelos (ativos e inativos). Uso administrativo.
    /// </summary>
    [HttpGet("providers")]
    public async Task<IActionResult> GetAllProviders(CancellationToken cancellationToken)
    {
        var providers = await _managementService.GetAllProvidersAsync(cancellationToken);
        return Ok(providers);
    }

    /// <summary>
    /// Adiciona um novo provider de IA.
    /// </summary>
    [HttpPost("providers")]
    public async Task<IActionResult> AddProvider([FromBody] AddAiProviderRequest request, CancellationToken cancellationToken)
    {
        var (success, message, id) = await _managementService.AddProviderAsync(request, cancellationToken);

        if (!success)
            return Conflict(new { message });

        return Created($"/api/v1/ai/management/providers", new { id, message });
    }

    /// <summary>
    /// Ativa ou desativa um provider.
    /// </summary>
    [HttpPatch("providers/{providerKey}/active")]
    public async Task<IActionResult> SetProviderActive(
        string providerKey,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var (success, message) = await _managementService.SetProviderActiveAsync(
            providerKey, request.IsActive, cancellationToken);

        if (!success)
            return NotFound(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Adiciona um novo modelo a um provider existente.
    /// </summary>
    [HttpPost("models")]
    public async Task<IActionResult> AddModel([FromBody] AddAiModelRequest request, CancellationToken cancellationToken)
    {
        var (success, message, id) = await _managementService.AddModelAsync(request, cancellationToken);

        if (!success)
            return Conflict(new { message });

        return Created($"/api/v1/ai/management/models", new { id, message });
    }

    /// <summary>
    /// Ativa ou desativa um modelo.
    /// </summary>
    [HttpPatch("providers/{providerKey}/models/{modelKey}/active")]
    public async Task<IActionResult> SetModelActive(
        string providerKey,
        string modelKey,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var (success, message) = await _managementService.SetModelActiveAsync(
            providerKey, modelKey, request.IsActive, cancellationToken);

        if (!success)
            return NotFound(new { message });

        return Ok(new { message });
    }
}
