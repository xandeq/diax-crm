namespace Diax.Domain.AI;

public interface IAiUsageLogRepository
{
    Task<AiUsageLog> CreateAsync(AiUsageLog log, CancellationToken cancellationToken = default);
    Task<List<AiUsageLog>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<AiUsageLog>> GetByProviderIdAsync(Guid providerId, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<AiUsageLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip, int take, CancellationToken cancellationToken = default);
    Task<(int totalLogs, int totalTokens, decimal totalCost)> GetUsageStatsAsync(Guid? userId = null, Guid? providerId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}
