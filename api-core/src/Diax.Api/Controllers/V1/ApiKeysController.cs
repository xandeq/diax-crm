using Asp.Versioning;
using Diax.Application.ApiKeys;
using Diax.Application.ApiKeys.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/apikeys")]
[ApiController]
[Authorize] // Apenas usuários autenticados via JWT podem gerenciar API Keys
public class ApiKeysController : BaseApiController
{
    private readonly ApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        ApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as API Keys (sem expor as chaves)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listando API Keys");
        var result = await _apiKeyService.GetAllAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém uma API Key por ID (sem expor a chave)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Obtendo API Key: {Id}", id);
        var result = await _apiKeyService.GetByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cria nova API Key (retorna plaintext APENAS aqui)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApiKeyRequest request,
        [FromServices] DiaxDbContext db,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Criando API Key: {Name}", request.Name);

        var userId = await ResolveUserIdAsync(db, cancellationToken);
        var userIdString = userId?.ToString() ?? "system";

        var result = await _apiKeyService.CreateAsync(request, userIdString, cancellationToken);

        if (result.IsFailure)
            return HandleResult(result);

        // IMPORTANTE: Esta é a ÚNICA vez que a chave plaintext é retornada
        return Ok(new
        {
            message = "API Key criada com sucesso. COPIE AGORA - não será exibida novamente.",
            apiKey = result.Value.PlainKey,
            id = result.Value.Id,
            name = result.Value.Name,
            expiresAt = result.Value.ExpiresAt,
            scope = result.Value.Scope
        });
    }

    /// <summary>
    /// Desabilita API Key
    /// </summary>
    [HttpPatch("{id}/disable")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disable(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Desabilitando API Key: {Id}", id);
        var result = await _apiKeyService.DisableAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Habilita API Key
    /// </summary>
    [HttpPatch("{id}/enable")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enable(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Habilitando API Key: {Id}", id);
        var result = await _apiKeyService.EnableAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deleta API Key permanentemente
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deletando API Key: {Id}", id);
        var result = await _apiKeyService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleResult(result);

        return NoContent();
    }
}
