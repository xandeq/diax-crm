using Diax.Domain.AI;

namespace Diax.Application.AI;

public class GroupAiAccessService : IGroupAiAccessService
{
    private readonly IGroupAiAccessRepository _repository;

    public GroupAiAccessService(IGroupAiAccessRepository repository)
    {
        _repository = repository;
    }

    public async Task<GroupAiAccessDto> GetGroupAccessAsync(Guid groupId)
    {
        var providerIds = await _repository.GetAllowedProviderIdsAsync(groupId);
        var modelIds = await _repository.GetAllowedModelIdsAsync(groupId);

        return new GroupAiAccessDto(groupId, providerIds, modelIds);
    }

    public async Task UpdateGroupAccessAsync(Guid groupId, UpdateGroupAiAccessRequest request)
    {
        await _repository.UpdateProviderAccessAsync(groupId, request.AllowedProviderIds);
        await _repository.UpdateModelAccessAsync(groupId, request.AllowedModelIds);
    }
}
