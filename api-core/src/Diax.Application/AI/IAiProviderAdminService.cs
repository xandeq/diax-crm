using Diax.Application.AI.Dtos;
using Diax.Domain.AI;

namespace Diax.Application.AI;

public interface IAiProviderAdminService
{
    Task<IEnumerable<AiProviderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AiProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiProviderDto> CreateAsync(CreateAiProviderRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateAiProviderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<AiModelDto>> GetModelsByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<AiModelDto> AddModelAsync(Guid providerId, AiModelDto modelDto, CancellationToken cancellationToken = default);
    Task UpdateModelsBatchAsync(Guid providerId, List<DiscoveredModelDto> models, CancellationToken cancellationToken = default);
    Task UpdateModelAsync(Guid modelId, AiModelDto modelDto, CancellationToken cancellationToken = default);
    Task DeleteModelAsync(Guid modelId, CancellationToken cancellationToken = default);

    Task<SyncModelsResultDto> SyncModelsAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discover available models from provider's API (currently supports OpenRouter only)
    /// </summary>
    Task<IEnumerable<DiscoveredModelDto>> DiscoverModelsAsync(string providerKey, CancellationToken cancellationToken = default);

    // ===== API Key Management =====

    /// <summary>
    /// Salva API key criptografada para um provider
    /// </summary>
    Task SaveApiKeyAsync(Guid providerId, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém status da credencial (se configurada + últimos 4 dígitos)
    /// </summary>
    Task<CredentialStatusDto> GetCredentialStatusAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Testa conexão com provider usando API key configurada
    /// </summary>
    Task<TestConnectionResultDto> TestConnectionAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém API key descriptografada (uso interno apenas)
    /// </summary>
    Task<string?> GetDecryptedApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default);
}
