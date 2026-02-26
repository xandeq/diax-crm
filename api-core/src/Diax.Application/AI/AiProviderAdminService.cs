using Diax.Application.AI.Dtos;
using Diax.Application.AI.Services;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI;

public class AiProviderAdminService : IAiProviderAdminService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IAiProviderCredentialRepository _credentialRepository;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenRouterClient _openRouterClient;
    private readonly IOpenAiClient _openAiClient;
    private readonly IGeminiClient _geminiClient;
    private readonly IDeepSeekModelClient _deepSeekModelClient;
    private readonly IAnthropicClient _anthropicClient;
    private readonly IGrokClient _grokClient;
    private readonly IMemoryCache _cache;
    private readonly PromptGeneratorSettings _promptSettings;
    private readonly ILogger<AiProviderAdminService> _logger;

    private const string CacheKeyDiscoveredModels = "ai-discovered-models:{0}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AiProviderAdminService(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IAiProviderCredentialRepository credentialRepository,
        IApiKeyEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        IOpenRouterClient openRouterClient,
        IOpenAiClient openAiClient,
        IGeminiClient geminiClient,
        IDeepSeekModelClient deepSeekModelClient,
        IAnthropicClient anthropicClient,
        IGrokClient grokClient,
        IMemoryCache cache,
        PromptGeneratorSettings promptSettings,
        ILogger<AiProviderAdminService> logger)
    {
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _openRouterClient = openRouterClient;
        _openAiClient = openAiClient;
        _geminiClient = geminiClient;
        _deepSeekModelClient = deepSeekModelClient;
        _anthropicClient = anthropicClient;
        _grokClient = grokClient;
        _cache = cache;
        _promptSettings = promptSettings;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        if (provider == null) return; // Or throw

        await _providerRepository.DeleteAsync(provider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteModelAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null) return;

        await _modelRepository.DeleteAsync(model, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
            model.MaxTokensHint,
            model.SupportsImageGeneration(),
            model.SupportsTextGeneration()
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
                "anthropic" => await DiscoverAnthropicModelsAsync(cancellationToken),
                "grok" => await DiscoverGrokModelsAsync(cancellationToken),
                "falai" => GetFalAiModels(),
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

    private async Task<List<DiscoveredModelDto>> DiscoverAnthropicModelsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AiProviderAdmin] Discovering Anthropic models");

        try
        {
            var response = await _anthropicClient.GetModelsAsync(cancellationToken);

            return response.Data
                .Select(m => new DiscoveredModelDto(
                    Id: m.Id,
                    Name: m.DisplayName,
                    Provider: "anthropic",
                    ContextLength: GetAnthropicContextLength(m.Id),
                    InputCostHint: null,  // Anthropic doesn't expose pricing via API
                    OutputCostHint: null
                ))
                .OrderByDescending(m => m.Id)  // Newest first
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiProviderAdmin] Failed to discover Anthropic models");
            throw new InvalidOperationException(
                "Failed to connect to Anthropic API. Please verify your API key is valid.", ex);
        }
    }

    private static int? GetAnthropicContextLength(string modelId)
    {
        // Context lengths based on Anthropic documentation
        if (modelId.Contains("claude-3-5")) return 200_000;
        if (modelId.Contains("claude-3")) return 200_000;
        if (modelId.Contains("claude-2.1")) return 100_000;
        return null;
    }

    private async Task<List<DiscoveredModelDto>> DiscoverGrokModelsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AiProviderAdmin] Discovering Grok models");

        try
        {
            var response = await _grokClient.GetModelsAsync(cancellationToken);

            return response.Data
                .Select(m => new DiscoveredModelDto(
                    Id: m.Id,
                    Name: FormatGrokModelName(m.Id),
                    Provider: "grok",
                    ContextLength: GetGrokContextLength(m.Id),
                    InputCostHint: null,  // Grok doesn't expose pricing via API
                    OutputCostHint: null
                ))
                .OrderByDescending(m => m.Id)  // Newest first (grok-4 before grok-3)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiProviderAdmin] Failed to discover Grok models");
            throw new InvalidOperationException(
                "Failed to connect to Grok API. Please verify your API key is valid.", ex);
        }
    }

    private static string FormatGrokModelName(string modelId)
    {
        // Convert "grok-4-1-fast-reasoning" → "Grok 4.1 Fast (Reasoning)"
        return modelId switch
        {
            "grok-4-1-fast-reasoning" => "Grok 4.1 Fast (Reasoning)",
            "grok-4-1-fast-non-reasoning" => "Grok 4.1 Fast (Non-Reasoning)",
            "grok-4-1-fast" => "Grok 4.1 Fast",
            "grok-4-1-fast-latest" => "Grok 4.1 Fast (Latest)",
            "grok-code-fast-1" => "Grok Code Fast 1",
            "grok-4" => "Grok 4",
            "grok-4-fast" => "Grok 4 Fast",
            "grok-3" => "Grok 3",
            "grok-3-mini" => "Grok 3 Mini",
            "grok-2-vision" => "Grok 2 Vision",
            _ => modelId  // Fallback to original ID
        };
    }

    private static int? GetGrokContextLength(string modelId)
    {
        // Context lengths based on xAI documentation
        if (modelId.Contains("grok-4")) return 2_000_000;  // 2M tokens
        if (modelId.Contains("grok-code")) return 256_000;  // 256K tokens
        if (modelId.Contains("grok-3")) return 128_000;  // Estimate
        if (modelId.Contains("grok-2")) return 128_000;  // Estimate
        return null;
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

    private static List<DiscoveredModelDto> GetFalAiModels()
    {
        return new List<DiscoveredModelDto>
        {
            new(Id: "fal-ai/flux/dev/image-to-image", Name: "Flux Dev (Image-to-Image)", Provider: "falai"),
            new(Id: "fal-ai/fast-sdxl", Name: "Fast SDXL", Provider: "falai"),
            new(Id: "fal-ai/flux/dev", Name: "Flux Dev", Provider: "falai"),
            new(Id: "fal-ai/flux-pro/v1.1", Name: "Flux Pro v1.1", Provider: "falai"),
            new(Id: "fal-ai/luma-dream-machine", Name: "Luma Dream Machine", Provider: "falai")
        };
    }

    // ===== API Key Management =====

    public async Task SaveApiKeyAsync(Guid providerId, string apiKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AiProviderAdmin] Saving API key for provider: {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {providerId} not found.");
        }

        // Encrypt API key
        var encrypted = _encryptionService.Encrypt(apiKey);
        var lastFour = _encryptionService.GetLastFourDigits(apiKey);

        // Save to database
        await _credentialRepository.CreateOrUpdateAsync(providerId, encrypted, lastFour, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[AiProviderAdmin] API key saved successfully for provider: {ProviderKey}", provider.Key);
    }

    public async Task<CredentialStatusDto> GetCredentialStatusAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null) return new CredentialStatusDto(IsConfigured: false, LastFourDigits: null);

        var credential = await _credentialRepository.GetByProviderIdAsync(providerId, cancellationToken);

        if (credential != null && credential.IsConfigured())
        {
            return new CredentialStatusDto(
                IsConfigured: true,
                LastFourDigits: credential.ApiKeyLastFourDigits
            );
        }

        // Fallback to appsettings
        var providerConfig = _promptSettings.GetProviderConfig(provider.Key);
        if (providerConfig != null && !string.IsNullOrWhiteSpace(providerConfig.ApiKey))
        {
            var lastFour = providerConfig.ApiKey.Length > 4
                ? providerConfig.ApiKey.Substring(providerConfig.ApiKey.Length - 4)
                : "****";
            return new CredentialStatusDto(
                IsConfigured: true,
                LastFourDigits: lastFour
            );
        }

        return new CredentialStatusDto(IsConfigured: false, LastFourDigits: null);
    }

    public async Task<TestConnectionResultDto> TestConnectionAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AiProviderAdmin] Testing connection for provider: {ProviderId}", providerId);

        try
        {
            // Get provider
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
            {
                return new TestConnectionResultDto(
                    Success: false,
                    Message: "Provider not found",
                    ErrorDetails: $"Provider with ID {providerId} does not exist"
                );
            }

            // Get credential
            var apiKey = await GetDecryptedApiKeyAsync(providerId, cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new TestConnectionResultDto(
                    Success: false,
                    Message: "API key not configured",
                    ErrorDetails: "Please configure an API key before testing the connection"
                );
            }

            // Test connection based on provider
            var testResult = provider.Key.ToLowerInvariant() switch
            {
                "openrouter" => await TestOpenRouterConnectionAsync(apiKey, cancellationToken),
                "openai" => await TestOpenAiConnectionAsync(apiKey, cancellationToken),
                "gemini" => await TestGeminiConnectionAsync(apiKey, cancellationToken),
                "deepseek" => await TestDeepSeekConnectionAsync(apiKey, cancellationToken),
                "anthropic" => await TestAnthropicConnectionAsync(apiKey, cancellationToken),
                "grok" => await TestGrokConnectionAsync(apiKey, cancellationToken),
                "perplexity" => new TestConnectionResultDto(
                    Success: true,
                    Message: "Perplexity API key configured (validation not implemented)"
                ),
                _ => new TestConnectionResultDto(
                    Success: false,
                    Message: "Connection test not supported for this provider",
                    ErrorDetails: $"Provider '{provider.Key}' does not have connection test implementation"
                )
            };

            _logger.LogInformation(
                "[AiProviderAdmin] Connection test for provider {ProviderKey}: {Success}",
                provider.Key,
                testResult.Success);

            return testResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiProviderAdmin] Failed to test connection for provider: {ProviderId}", providerId);
            return new TestConnectionResultDto(
                Success: false,
                Message: "Connection test failed",
                ErrorDetails: ex.Message
            );
        }
    }

    public async Task<string?> GetDecryptedApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByProviderIdAsync(providerId, cancellationToken);

        if (credential == null || !credential.IsConfigured())
        {
            return null;
        }

        try
        {
            return _encryptionService.Decrypt(credential.ApiKeyEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiProviderAdmin] Failed to decrypt API key for provider: {ProviderId}", providerId);
            return null;
        }
    }

    // ===== Connection Test Helpers =====

    private async Task<TestConnectionResultDto> TestOpenRouterConnectionAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            // Test by calling models endpoint (lightweight operation)
            var response = await _openRouterClient.GetModelsAsync(cancellationToken);
            return new TestConnectionResultDto(
                Success: true,
                Message: $"Connection successful - {response.Data.Count} models available"
            );
        }
        catch (Exception ex)
        {
            return new TestConnectionResultDto(
                Success: false,
                Message: "OpenRouter connection failed",
                ErrorDetails: ex.Message
            );
        }
    }

    private async Task<TestConnectionResultDto> TestOpenAiConnectionAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _openAiClient.GetModelsAsync(cancellationToken);
            return new TestConnectionResultDto(
                Success: true,
                Message: $"Connection successful - {response.Data.Count} models available"
            );
        }
        catch (Exception ex)
        {
            return new TestConnectionResultDto(
                Success: false,
                Message: "OpenAI connection failed",
                ErrorDetails: ex.Message
            );
        }
    }

    private async Task<TestConnectionResultDto> TestGeminiConnectionAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _geminiClient.GetModelsAsync(cancellationToken);
            return new TestConnectionResultDto(
                Success: true,
                Message: $"Connection successful - {response.Models.Count} models available"
            );
        }
        catch (Exception ex)
        {
            return new TestConnectionResultDto(
                Success: false,
                Message: "Gemini connection failed",
                ErrorDetails: ex.Message
            );
        }
    }

    private async Task<TestConnectionResultDto> TestDeepSeekConnectionAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _deepSeekModelClient.GetModelsAsync(cancellationToken);
            return new TestConnectionResultDto(
                Success: true,
                Message: $"Connection successful - {response.Data.Count} models available"
            );
        }
        catch (Exception ex)
        {
            return new TestConnectionResultDto(
                Success: false,
                Message: "DeepSeek connection failed",
                ErrorDetails: ex.Message
            );
        }
    }

    private async Task<TestConnectionResultDto> TestAnthropicConnectionAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            // Use temporary client with provided API key
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.anthropic.com/v1/"),
                Timeout = TimeSpan.FromSeconds(10)
            };

            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            _logger.LogInformation("[AiProviderAdmin] Testing Anthropic connection");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = System.Text.Json.JsonSerializer.Deserialize<Dtos.AnthropicModelsResponse>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var modelCount = result?.Data?.Count ?? 0;

                return new TestConnectionResultDto(
                    Success: true,
                    Message: $"Successfully connected to Anthropic API - {modelCount} models available"
                );
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return new TestConnectionResultDto(
                Success: false,
                Message: "Failed to connect to Anthropic API",
                ErrorDetails: $"HTTP {response.StatusCode}: {errorBody}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiProviderAdmin] Anthropic connection test failed");

            return new TestConnectionResultDto(
                Success: false,
                Message: "Connection test failed",
                ErrorDetails: ex.Message
            );
        }
    }

    private async Task<TestConnectionResultDto> TestGrokConnectionAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            // Use temporary client with provided API key
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.x.ai/v1/"),
                Timeout = TimeSpan.FromSeconds(10)
            };

            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            _logger.LogInformation("[AiProviderAdmin] Testing Grok connection");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = System.Text.Json.JsonSerializer.Deserialize<Dtos.GrokModelsResponse>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var modelCount = result?.Data?.Count ?? 0;

                return new TestConnectionResultDto(
                    Success: true,
                    Message: $"Successfully connected to Grok API - {modelCount} models available"
                );
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return new TestConnectionResultDto(
                Success: false,
                Message: "Failed to connect to Grok API",
                ErrorDetails: $"HTTP {response.StatusCode}: {errorBody}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiProviderAdmin] Grok connection test failed");

            return new TestConnectionResultDto(
                Success: false,
                Message: "Connection test failed",
                ErrorDetails: ex.Message
            );
        }
    }
}
