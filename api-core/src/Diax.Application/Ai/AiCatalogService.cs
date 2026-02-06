using Diax.Application.AI.Dtos;
using Diax.Domain.AI;
using Diax.Domain.UserGroups;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI;

/// <summary>
/// Serviço de consulta de providers e modelos de IA.
///
/// FONTE ÚNICA DE VERDADE: Banco de dados.
///
/// Este serviço NÃO possui fallback hardcoded. Se o banco retornar vazio,
/// isso é tratado como um ERRO CRÍTICO que deve ser investigado.
/// </summary>
public class AiCatalogService : IAiCatalogService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IUserGroupRepository _userGroupRepository;
    private readonly IGroupAiAccessRepository _accessRepository;
    private readonly ILogger<AiCatalogService> _logger;

    public AiCatalogService(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IUserGroupRepository userGroupRepository,
        IGroupAiAccessRepository accessRepository,
        ILogger<AiCatalogService> logger)
    {
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _userGroupRepository = userGroupRepository;
        _accessRepository = accessRepository;
        _logger = logger;
    }

    public async Task<List<AiProviderDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AiCatalog] Building AI catalog (no user filtering)");

        try
        {
            var allProviders = await _providerRepository.GetAllIncludedAsync(cancellationToken);

            if (allProviders == null || !allProviders.Any())
            {
                _logger.LogCritical(
                    "[AiCatalog] CRITICAL: Nenhum AI Provider encontrado no banco de dados. " +
                    "Verifique se o AiDataSeeder foi executado. " +
                    "Banco de dados é a ÚNICA fonte de verdade — nenhum fallback será usado.");
                return new List<AiProviderDto>();
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
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[AiCatalog] CRITICAL: Falha ao consultar providers no banco de dados. " +
                "Nenhum fallback hardcoded será usado. Frontend receberá lista vazia.");
            return new List<AiProviderDto>();
        }
    }

    public async Task<List<AiProviderDto>> GetUserCatalogAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AiCatalog] Building catalog for user {UserId}", userId);

        try
        {
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
                _logger.LogCritical(
                    "[AiCatalog] CRITICAL: Nenhum provider no banco para user {UserId}. " +
                    "Banco de dados é a ÚNICA fonte de verdade.", userId);
                return new List<AiProviderDto>();
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
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[AiCatalog] CRITICAL: Erro ao montar catálogo para user {UserId}.", userId);
            return new List<AiProviderDto>();
        }
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
}
