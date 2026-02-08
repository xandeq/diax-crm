using Diax.Application.AI.Dtos;
using Diax.Domain.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI;

public class AiProviderAdminService : IAiProviderAdminService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IOpenRouterClient _openRouterClient;
    private readonly IOpenAiClient _openAiClient;
    private readonly IGeminiClient _geminiClient;
    private readonly IDeepSeekModelClient _deepSeekModelClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiProviderAdminService> _logger;

    private const string CacheKeyDiscoveredModels = "ai-discovered-models:{0}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AiProviderAdminService(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IOpenRouterClient openRouterClient,
        IOpenAiClient openAiClient,
        IGeminiClient geminiClient,
        IDeepSeekModelClient deepSeekModelClient,
        IMemoryCache cache,
        ILogger<AiProviderAdminService> logger)
    {
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _openRouterClient = openRouterClient;
        _openAiClient = openAiClient;
        _geminiClient = geminiClient;
        _deepSeekModelClient = deepSeekModelClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<AiProviderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _providerRepository.GetAllIncludedAsync(cancellationToken);
        return providers.Select(MapToDto);
    }

    public async Task<AiProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        if (provider == null) return null;

        // Ensure models are loaded if not included by GetByIdAsync (Repository<T> usually finds by key/id only)
        // Check if models are loaded or load them
        var models = await _modelRepository.GetByProviderIdAsync(id, cancellationToken);
        // We can't set the collection on privacy encapsulated entity easily without method,
        // but for DTO mapping we can use the list we just fetched.

        return MapToDto(provider, models);
    }

    public async Task<AiProviderDto> CreateAsync(CreateAiProviderRequest request, CancellationToken cancellationToken = default)
    {
        // Check if key exists
        var existing = await _providerRepository.GetByKeyAsync(request.Key, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Provider with key '{request.Key}' already exists.");
        }

        var provider = new AiProvider(request.Key, request.Name, request.SupportsListModels, request.BaseUrl);
        await _providerRepository.AddAsync(provider, cancellationToken);

        return MapToDto(provider);
    }

    public async Task UpdateAsync(Guid id, UpdateAiProviderRequest request, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        if (provider == null) throw new KeyNotFoundException($"Provider with ID {id} not found.");

        provider.UpdateDetails(request.Name, request.SupportsListModels, request.BaseUrl);

        if (request.IsEnabled) provider.Enable();
        else provider.Disable();

        await _providerRepository.UpdateAsync(provider, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        if (provider == null) return; // Or throw

        await _providerRepository.DeleteAsync(provider, cancellationToken);
    }

    public async Task<IEnumerable<AiModelDto>> GetModelsByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var models = await _modelRepository.GetByProviderIdAsync(providerId, cancellationToken);
        return models.Select(MapToDto);
    }

    public async Task<AiModelDto> AddModelAsync(Guid providerId, AiModelDto modelDto, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null) throw new KeyNotFoundException($"Provider with ID {providerId} not found.");

        var model = new AiModel(providerId, modelDto.ModelKey, modelDto.DisplayName, modelDto.IsDiscovered);
        model.UpdateDetails(modelDto.DisplayName, modelDto.InputCostHint, modelDto.OutputCostHint, modelDto.MaxTokensHint, null);

        if (modelDto.IsEnabled) model.Enable();
        else model.Disable();

        await _modelRepository.AddAsync(model, cancellationToken);

        return MapToDto(model);
    }

    public async Task UpdateModelsBatchAsync(Guid providerId, List<DiscoveredModelDto> models, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null) throw new KeyNotFoundException($"Provider with ID {providerId} not found.");

        var existingModels = await _modelRepository.GetByProviderIdAsync(providerId, cancellationToken);
        var existingKeys = existingModels.ToDictionary(m => m.ModelKey.ToLowerInvariant(), m => m);

        foreach (var discovered in models)
        {
            var key = discovered.Id.ToLowerInvariant();
            if (existingKeys.TryGetValue(key, out var existing))
            {
                // Update existing
                // If it was discovered but not enabled, we enable it now since user selected it
                existing.Enable();
                existing.UpdateDetails(
                    discovered.Name,
                    TryGetDecimal(discovered.InputCostHint),
                    TryGetDecimal(discovered.OutputCostHint),
                    discovered.ContextLength,
                    existing.CapabilitiesJson
                );
                await _modelRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                // Add new
                var model = new AiModel(providerId, discovered.Id, discovered.Name, isDiscovered: true);
                model.Enable();
                model.UpdateDetails(
                    discovered.Name,
                    TryGetDecimal(discovered.InputCostHint),
                    TryGetDecimal(discovered.OutputCostHint),
                    discovered.ContextLength,
                    null
                );
                await _modelRepository.AddAsync(model, cancellationToken);
            }
        }
    }

    private static decimal? TryGetDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    public async Task UpdateModelAsync(Guid modelId, AiModelDto modelDto, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null) throw new KeyNotFoundException($"Model with ID {modelId} not found.");

        model.UpdateDetails(modelDto.DisplayName, modelDto.InputCostHint, modelDto.OutputCostHint, modelDto.MaxTokensHint, null);

        if (modelDto.IsEnabled) model.Enable();
        else model.Disable();

        await _modelRepository.UpdateAsync(model, cancellationToken);
    }

    public async Task DeleteModelAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null) return;

        await _modelRepository.DeleteAsync(model, cancellationToken);
    }

    public async Task<SyncModelsResultDto> SyncModelsAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null) throw new KeyNotFoundException($"Provider with ID {providerId} not found.");

        if (!provider.SupportsListModels)
        {
            throw new InvalidOperationException($"Provider '{provider.Name}' does not support model listing.");
        }

        // TODO: Implement actual API call logic using HttpClientFactory and provider specific clients.
        // For now, we return a mock result to allow frontend development to proceed.

        return new SyncModelsResultDto(
            DiscoveredCount: 0,
            NewModels: 0,
            ExistingModelsUpdated: 0,
            Errors: new List<string> { "Sync implementation pending integration with HTTP clients." }
        );
    }

    private static AiProviderDto MapToDto(AiProvider provider)
    {
        return MapToDto(provider, provider.Models);
    }

    private static AiProviderDto MapToDto(AiProvider provider, IEnumerable<AiModel> models)
    {
        return new AiProviderDto(
            provider.Id,
            provider.Key,
            provider.Name,
            provider.IsEnabled,
            provider.SupportsListModels,
            provider.BaseUrl,
            models?.Select(MapToDto).ToList() ?? new List<AiModelDto>()
        );
    }

    private static AiModelDto MapToDto(AiModel model)
    {
        return new AiModelDto(
            model.Id,
            model.ModelKey,
            model.DisplayName,
            model.IsEnabled,
            model.IsDiscovered,
            model.InputCostHint,
            model.OutputCostHint,
            model.MaxTokensHint
        );
    }

    public async Task<IEnumerable<DiscoveredModelDto>> DiscoverModelsAsync(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(CacheKeyDiscoveredModels, providerKey.ToLowerInvariant());

        // Try get from cache
        if (_cache.TryGetValue<IEnumerable<DiscoveredModelDto>>(cacheKey, out var cachedModels) && cachedModels != null)
        {
            _logger.LogInformation("Returning cached discovered models for provider: {Provider}", providerKey);
            return cachedModels;
        }

        _logger.LogInformation("Discovering models for provider: {Provider}", providerKey);

        try
        {
            var discoveredModels = providerKey.ToLowerInvariant() switch
            {
                "openrouter" => await DiscoverOpenRouterModelsAsync(cancellationToken),
                "openai" => await DiscoverOpenAiModelsAsync(cancellationToken),
                "gemini" => await DiscoverGeminiModelsAsync(cancellationToken),
                "deepseek" => await DiscoverDeepSeekModelsAsync(cancellationToken),
                "perplexity" => GetPerplexityModels(),
                _ => throw new NotSupportedException(
                    $"Model discovery is not yet supported for provider '{providerKey}'.")
            };

            // Cache the result
            _cache.Set(cacheKey, discoveredModels, CacheDuration);

            _logger.LogInformation(
                "Successfully discovered {Count} models for provider: {Provider}",
                discoveredModels.Count,
                providerKey);

            return discoveredModels;
        }
        catch (NotSupportedException)
        {
            _logger.LogWarning("Model discovery not supported for provider: {Provider}", providerKey);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover models for provider: {Provider}", providerKey);
            throw;
        }
    }

    private async Task<List<DiscoveredModelDto>> DiscoverOpenRouterModelsAsync(CancellationToken cancellationToken)
    {
        var response = await _openRouterClient.GetModelsAsync(cancellationToken);

        return response.Data
            .Select(m => new DiscoveredModelDto(
                Id: m.Id,
                Name: m.Name,
                Provider: "openrouter",
                ContextLength: m.ContextLength,
                InputCostHint: m.Pricing?.Prompt,
                OutputCostHint: m.Pricing?.Completion
            ))
            .OrderBy(m => m.Name)
            .ToList();
    }

    private async Task<List<DiscoveredModelDto>> DiscoverOpenAiModelsAsync(CancellationToken cancellationToken)
    {
        var response = await _openAiClient.GetModelsAsync(cancellationToken);

        return response.Data
            .Select(m => new DiscoveredModelDto(
                Id: m.Id,
                Name: m.Id,  // OpenAI API only provides id, no friendly name
                Provider: "openai"
            ))
            .OrderBy(m => m.Id)
            .ToList();
    }

    private async Task<List<DiscoveredModelDto>> DiscoverGeminiModelsAsync(CancellationToken cancellationToken)
    {
        var response = await _geminiClient.GetModelsAsync(cancellationToken);

        return response.Models
            .Where(m => m.SupportedGenerationMethods.Contains("generateContent"))
            .Select(m => new DiscoveredModelDto(
                Id: m.Name.StartsWith("models/") ? m.Name["models/".Length..] : m.Name,
                Name: m.DisplayName,
                Provider: "gemini",
                ContextLength: m.InputTokenLimit
            ))
            .OrderBy(m => m.Name)
            .ToList();
    }

    private async Task<List<DiscoveredModelDto>> DiscoverDeepSeekModelsAsync(CancellationToken cancellationToken)
    {
        var response = await _deepSeekModelClient.GetModelsAsync(cancellationToken);

        return response.Data
            .Select(m => new DiscoveredModelDto(
                Id: m.Id,
                Name: m.Id,  // DeepSeek API only provides id, no friendly name
                Provider: "deepseek"
            ))
            .OrderBy(m => m.Id)
            .ToList();
    }

    private static List<DiscoveredModelDto> GetPerplexityModels()
    {
        return new List<DiscoveredModelDto>
        {
            new(Id: "sonar", Name: "Sonar", Provider: "perplexity"),
            new(Id: "sonar-pro", Name: "Sonar Pro", Provider: "perplexity"),
            new(Id: "sonar-deep-research", Name: "Sonar Deep Research", Provider: "perplexity"),
            new(Id: "sonar-reasoning", Name: "Sonar Reasoning", Provider: "perplexity"),
            new(Id: "sonar-reasoning-pro", Name: "Sonar Reasoning Pro", Provider: "perplexity"),
            new(Id: "r1-1776", Name: "R1-1776", Provider: "perplexity"),
        };
    }
}
