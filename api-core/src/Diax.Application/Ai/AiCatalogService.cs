using Diax.Application.AI.Dtos;
using Diax.Domain.AI;
using Diax.Domain.UserGroups;

namespace Diax.Application.AI;

public class AiCatalogService : IAiCatalogService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IUserGroupRepository _userGroupRepository;
    private readonly IGroupAiAccessRepository _accessRepository;

    public AiCatalogService(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IUserGroupRepository userGroupRepository,
        IGroupAiAccessRepository accessRepository)
    {
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _userGroupRepository = userGroupRepository;
        _accessRepository = accessRepository;
    }

    public async Task<List<AiProviderDto>> GetUserCatalogAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Get User Groups
        var userGroups = await _userGroupRepository.GetByUserIdAsync(userId, cancellationToken);
        var userGroupIds = userGroups.Select(g => g.Id).ToList();

        // 2. Get All Enabled Providers
        var allProviders = await _providerRepository.GetAllIncludedAsync(cancellationToken);
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
}
