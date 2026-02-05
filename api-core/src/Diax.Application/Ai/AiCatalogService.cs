using Diax.Application.AI.Dtos;
using Diax.Domain.AI;
using Diax.Domain.UserGroups;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI;

public class AiCatalogService : IAiCatalogService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IUserGroupRepository _userGroupRepository;
    private readonly IGroupAiAccessRepository _accessRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiCatalogService> _logger;

    public AiCatalogService(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IUserGroupRepository userGroupRepository,
        IGroupAiAccessRepository accessRepository,
        IConfiguration configuration,
        ILogger<AiCatalogService> logger)
    {
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _userGroupRepository = userGroupRepository;
        _accessRepository = accessRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<AiProviderDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AiCatalog] Building AI catalog (no user filtering)");

        // Tentar buscar providers do banco
        var allProviders = await _providerRepository.GetAllIncludedAsync(cancellationToken);
        
        if (allProviders == null || !allProviders.Any())
        {
            _logger.LogWarning("[AiCatalog] No providers found in database. Using configuration fallback.");
            return BuildProvidersFromConfiguration();
        }

        _logger.LogInformation("[AiCatalog] Found {Count} providers in database", allProviders.Count());
        
        var result = new List<AiProviderDto>();
        foreach (var provider in allProviders)
        {
            if (!provider.IsEnabled) continue;

            var models = provider.Models
                .Where(m => m.IsEnabled)
                .Select(MapToDto)
                .ToList();

            if (models.Any())
            {
                result.Add(new AiProviderDto(
                    provider.Id,
                    provider.Key,
                    provider.Name,
                    provider.IsEnabled,
                    provider.SupportsListModels,
                    provider.BaseUrl,
                    models
                ));
            }
        }

        _logger.LogInformation("[AiCatalog] Returning {Count} enabled providers", result.Count);
        return result;
    }

    public async Task<List<AiProviderDto>> GetUserCatalogAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AiCatalog] Building catalog for user {UserId}", userId);

        // 1. Get User Groups
        var userGroups = await _userGroupRepository.GetByUserIdAsync(userId, cancellationToken);
        var userGroupIds = userGroups.Select(g => g.Id).ToList();

        if (!userGroupIds.Any())
        {
            _logger.LogWarning("[AiCatalog] User {UserId} has no groups. Using general catalog.", userId);
            return await GetCatalogAsync(cancellationToken);
        }

        // 2. Get All Enabled Providers
        var allProviders = await _providerRepository.GetAllIncludedAsync(cancellationToken);
        
        if (allProviders == null || !allProviders.Any())
        {
            _logger.LogWarning("[AiCatalog] No providers in database for user {UserId}. Using configuration fallback.", userId);
            return BuildProvidersFromConfiguration();
        }

        var result = new List<AiProviderDto>();

        foreach (var provider in allProviders)
        {
            if (!provider.IsEnabled) continue;

            // Check Provider Access
            var hasProviderAccess = false;
            foreach (var groupId in userGroupIds)
            {
                if (await _accessRepository.HasProviderAccessAsync(groupId, provider.Id, cancellationToken))
                {
                    hasProviderAccess = true;
                    break;
                }
            }

            if (!hasProviderAccess) continue;

            // Check Models
            var validModels = new List<AiModelDto>();
            foreach (var model in provider.Models)
            {
                if (!model.IsEnabled) continue;

                var hasModelAccess = false;
                foreach (var groupId in userGroupIds)
                {
                    if (await _accessRepository.HasModelAccessAsync(groupId, model.Id, cancellationToken))
                    {
                        hasModelAccess = true;
                        break;
                    }
                }

                if (hasModelAccess)
                {
                    validModels.Add(MapToDto(model));
                }
            }

            if (validModels.Any())
            {
                result.Add(new AiProviderDto(
                    provider.Id,
                    provider.Key,
                    provider.Name,
                    provider.IsEnabled,
                    provider.SupportsListModels,
                    provider.BaseUrl,
                    validModels
                ));
            }
        }

        return result;
    }

    public async Task<bool> ValidateUserAccessAsync(Guid userId, string providerKey, string modelKey, CancellationToken cancellationToken = default)
    {
        // 1. Get User Groups
        var userGroups = await _userGroupRepository.GetByUserIdAsync(userId, cancellationToken);
        var userGroupIds = userGroups.Select(g => g.Id).ToList();

        // 2. Resolve Provider and Model by Key
        var provider = await _providerRepository.GetByKeyAsync(providerKey, cancellationToken);

        if (provider == null || !provider.IsEnabled) return false;

        var models = await _modelRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
        var model = models.FirstOrDefault(m => m.ModelKey == modelKey);

        if (model == null || !model.IsEnabled) return false;

        // 3. Check Access
        var hasProviderAccess = false;
        foreach (var groupId in userGroupIds)
        {
            if (await _accessRepository.HasProviderAccessAsync(groupId, provider.Id, cancellationToken))
            {
                hasProviderAccess = true;
                break;
            }
        }

        if (!hasProviderAccess) return false;

        var hasModelAccess = false;
        foreach (var groupId in userGroupIds)
        {
            if (await _accessRepository.HasModelAccessAsync(groupId, model.Id, cancellationToken))
            {
                hasModelAccess = true;
                break;
            }
        }

        return hasModelAccess;
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

    private List<AiProviderDto> BuildProvidersFromConfiguration()
    {
        _logger.LogInformation("[AiCatalog] Building providers from appsettings configuration");
        var providers = new List<AiProviderDto>();

        // OpenAI
        var openAiKey = _configuration["PromptGenerator:OpenAI:ApiKey"];
        _logger.LogDebug("[AiCatalog] OpenAI ApiKey configured: {IsConfigured}", !string.IsNullOrWhiteSpace(openAiKey));
        
        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            providers.Add(new AiProviderDto(
                Guid.NewGuid(),
                "openai",
                "OpenAI",
                true,
                false,
                _configuration["PromptGenerator:OpenAI:BaseUrl"],
                new List<AiModelDto>
                {
                    new(Guid.NewGuid(), "gpt-4o", "GPT-4o", true, false, null, null, null),
                    new(Guid.NewGuid(), "gpt-4o-mini", "GPT-4o Mini", true, false, null, null, null),
                    new(Guid.NewGuid(), "gpt-4-turbo", "GPT-4 Turbo", true, false, null, null, null),
                    new(Guid.NewGuid(), "gpt-3.5-turbo", "GPT-3.5 Turbo", true, false, null, null, null)
                }
            ));
        }

        // Anthropic Claude
        var anthropicKey = _configuration["PromptGenerator:Anthropic:ApiKey"] 
                          ?? _configuration["Anthropic:ApiKey"];
        _logger.LogDebug("[AiCatalog] Anthropic ApiKey configured: {IsConfigured}", !string.IsNullOrWhiteSpace(anthropicKey));
        
        if (!string.IsNullOrWhiteSpace(anthropicKey))
        {
            providers.Add(new AiProviderDto(
                Guid.NewGuid(),
                "anthropic",
                "Anthropic Claude",
                true,
                false,
                _configuration["PromptGenerator:Anthropic:BaseUrl"] ?? "https://api.anthropic.com",
                new List<AiModelDto>
                {
                    new(Guid.NewGuid(), "claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet", true, false, null, null, null),
                    new(Guid.NewGuid(), "claude-3-opus-20240229", "Claude 3 Opus", true, false, null, null, null),
                    new(Guid.NewGuid(), "claude-3-sonnet-20240229", "Claude 3 Sonnet", true, false, null, null, null),
                    new(Guid.NewGuid(), "claude-3-haiku-20240307", "Claude 3 Haiku", true, false, null, null, null)
                }
            ));
        }

        // Google Gemini
        var geminiKey = _configuration["PromptGenerator:Google:ApiKey"] 
                       ?? _configuration["Google:ApiKey"];
        _logger.LogDebug("[AiCatalog] Google ApiKey configured: {IsConfigured}", !string.IsNullOrWhiteSpace(geminiKey));
        
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            providers.Add(new AiProviderDto(
                Guid.NewGuid(),
                "google",
                "Google Gemini",
                true,
                false,
                _configuration["PromptGenerator:Google:BaseUrl"] ?? "https://generativelanguage.googleapis.com",
                new List<AiModelDto>
                {
                    new(Guid.NewGuid(), "gemini-2.0-flash-exp", "Gemini 2.0 Flash", true, false, null, null, null),
                    new(Guid.NewGuid(), "gemini-exp-1206", "Gemini Experimental", true, false, null, null, null),
                    new(Guid.NewGuid(), "gemini-1.5-pro", "Gemini 1.5 Pro", true, false, null, null, null),
                    new(Guid.NewGuid(), "gemini-1.5-flash", "Gemini 1.5 Flash", true, false, null, null, null)
                }
            ));
        }

        // Perplexity
        var perplexityKey = _configuration["PromptGenerator:Perplexity:ApiKey"];
        _logger.LogDebug("[AiCatalog] Perplexity ApiKey configured: {IsConfigured}", !string.IsNullOrWhiteSpace(perplexityKey));
        
        if (!string.IsNullOrWhiteSpace(perplexityKey))
        {
            providers.Add(new AiProviderDto(
                Guid.NewGuid(),
                "perplexity",
                "Perplexity",
                true,
                false,
                _configuration["PromptGenerator:Perplexity:BaseUrl"],
                new List<AiModelDto>
                {
                    new(Guid.NewGuid(), "sonar-pro", "Sonar Pro", true, false, null, null, null),
                    new(Guid.NewGuid(), "sonar", "Sonar", true, false, null, null, null)
                }
            ));
        }

        // DeepSeek
        var deepseekKey = _configuration["PromptGenerator:DeepSeek:ApiKey"];
        _logger.LogDebug("[AiCatalog] DeepSeek ApiKey configured: {IsConfigured}", !string.IsNullOrWhiteSpace(deepseekKey));
        
        if (!string.IsNullOrWhiteSpace(deepseekKey))
        {
            providers.Add(new AiProviderDto(
                Guid.NewGuid(),
                "deepseek",
                "DeepSeek",
                true,
                false,
                _configuration["PromptGenerator:DeepSeek:BaseUrl"],
                new List<AiModelDto>
                {
                    new(Guid.NewGuid(), "deepseek-chat", "DeepSeek Chat", true, false, null, null, null),
                    new(Guid.NewGuid(), "deepseek-reasoner", "DeepSeek Reasoner", true, false, null, null, null)
                }
            ));
        }

        if (providers.Count == 0)
        {
            _logger.LogWarning("[AiCatalog] ⚠️ No AI provider API keys configured in appsettings. Add keys to PromptGenerator section.");
        }
        else
        {
            _logger.LogInformation("[AiCatalog] Built {Count} providers from configuration: {Providers}", 
                providers.Count, 
                string.Join(", ", providers.Select(p => p.Name)));
        }

        return providers;
    }
}
