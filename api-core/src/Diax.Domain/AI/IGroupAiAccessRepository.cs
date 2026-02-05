using Diax.Domain.AI;

namespace Diax.Domain.AI;

public interface IGroupAiAccessRepository
{
    Task<List<Guid>> GetAllowedProviderIdsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetAllowedModelIdsAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task UpdateProviderAccessAsync(Guid groupId, List<Guid> providerIds, CancellationToken cancellationToken = default);
    Task UpdateModelAccessAsync(Guid groupId, List<Guid> modelIds, CancellationToken cancellationToken = default);

    Task<bool> HasProviderAccessAsync(Guid groupId, Guid providerId, CancellationToken cancellationToken = default);
    Task<bool> HasModelAccessAsync(Guid groupId, Guid modelId, CancellationToken cancellationToken = default);
}
