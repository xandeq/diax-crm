using Diax.Application.AI;
using Diax.Domain.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI;

/// <summary>
/// Valida providers e modelos de IA consultando o banco de dados com cache de 5 minutos.
/// O banco de dados é a ÚNICA fonte de verdade — nenhuma lista hardcoded é usada.
/// </summary>
public class AiModelValidator : IAiModelValidator
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiModelValidator> _logger;

    private const string CacheKeyProviders = "ai-validator-providers";
    private const string CacheKeyModelsPrefix = "ai-validator-models-";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AiModelValidator(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IMemoryCache cache,
        ILogger<AiModelValidator> logger)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsValidProviderAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
            return false;

        var providers = await GetActiveProviderKeysAsync(cancellationToken);
        return providers.Any(p => p.Equals(providerKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> IsValidModelAsync(string providerKey, string modelKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(modelKey))
            return false;

        var models = await GetActiveModelKeysAsync(providerKey, cancellationToken);
        return models.Any(m => m.Equals(modelKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<string>> GetActiveModelKeysAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyModelsPrefix}{providerKey?.ToLowerInvariant()}";

        var models = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            _logger.LogDebug("[AiModelValidator] Cache miss para modelos do provider '{Provider}'. Consultando banco.", providerKey);

            var provider = await _providerRepository.GetByKeyAsync(providerKey!, cancellationToken);
            if (provider == null || !provider.IsEnabled)
            {
                _logger.LogWarning("[AiModelValidator] Provider '{Provider}' não encontrado ou inativo.", providerKey);
                return Array.Empty<string>();
            }

            var allModels = await _modelRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
            var activeKeys = allModels
                .Where(m => m.IsEnabled)
                .Select(m => m.ModelKey)
                .OrderBy(k => k)
                .ToArray();

            if (activeKeys.Length == 0)
            {
                _logger.LogWarning("[AiModelValidator] Nenhum modelo ativo encontrado para o provider '{Provider}'.", providerKey);
            }

            return activeKeys;
        });

        return models ?? Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetActiveProviderKeysAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _cache.GetOrCreateAsync(CacheKeyProviders, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            _logger.LogDebug("[AiModelValidator] Cache miss para providers ativos. Consultando banco.");

            var allProviders = await _providerRepository.GetAllAsync(cancellationToken);
            var activeKeys = allProviders
                .Where(p => p.IsEnabled)
                .Select(p => p.Key)
                .OrderBy(k => k)
                .ToArray();

            if (activeKeys.Length == 0)
            {
                _logger.LogCritical("[AiModelValidator] CRITICAL: Nenhum AI Provider ativo encontrado no banco. Verifique o AiDataSeeder.");
            }

            return activeKeys;
        });

        return providers ?? Array.Empty<string>();
    }

    public void InvalidateCache()
    {
        _logger.LogInformation("[AiModelValidator] Cache invalidado manualmente.");
        _cache.Remove(CacheKeyProviders);
        // Modelos serão invalidados via TTL ou reinicialização
    }
}
