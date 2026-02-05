using Diax.Domain.AI;

namespace Diax.Application.AI;

public interface IGroupAiAccessService
{
    Task<GroupAiAccessDto> GetGroupAccessAsync(Guid groupId);
    Task UpdateGroupAccessAsync(Guid groupId, UpdateGroupAiAccessRequest request);
}

public record GroupAiAccessDto(Guid GroupId, List<Guid> AllowedProviderIds, List<Guid> AllowedModelIds);
public record UpdateGroupAiAccessRequest(List<Guid> AllowedProviderIds, List<Guid> AllowedModelIds);
