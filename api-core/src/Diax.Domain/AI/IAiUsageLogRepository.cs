using Diax.Domain.Common;

namespace Diax.Domain.AI;

public interface IAiUsageLogRepository : IRepository<AiUsageLog>
{
    Task<AiUsageSummary> GetSummaryAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? providerId,
        Guid? modelId,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public record AiUsageSummary(
    int TotalRequests,
    int TotalTokensInput,
    int TotalTokensOutput,
    int TotalTokens,
    decimal TotalCostEstimated,
    Dictionary<string, ProviderUsage> ByProvider
);

public record ProviderUsage(
    Guid ProviderId,
    string ProviderName,
    int RequestCount,
    decimal TotalCost
);
