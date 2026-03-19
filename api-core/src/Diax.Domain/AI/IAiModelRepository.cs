using Diax.Domain.Common;

namespace Diax.Domain.AI;

public interface IAiModelRepository : IRepository<AiModel>
{
    Task<IEnumerable<AiModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiModel>> GetEnabledByProviderAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<AiModel?> GetByProviderAndModelKeyAsync(Guid providerId, string modelKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists only the failure tracking fields (consecutive_failure_count, last_failure_at,
    /// last_success_at, last_failure_category, last_failure_message) for the given model.
    /// Used in fire-and-forget to avoid updating unrelated fields.
    /// </summary>
    Task UpdateFailureTrackingAsync(AiModel model, CancellationToken cancellationToken = default);
}
