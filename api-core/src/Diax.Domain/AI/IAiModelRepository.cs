using Diax.Domain.Common;

namespace Diax.Domain.AI;

public interface IAiModelRepository : IRepository<AiModel>
{
    Task<IEnumerable<AiModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiModel>> GetEnabledByProviderAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<AiModel?> GetByProviderAndModelKeyAsync(Guid providerId, string modelKey, CancellationToken cancellationToken = default);
}
