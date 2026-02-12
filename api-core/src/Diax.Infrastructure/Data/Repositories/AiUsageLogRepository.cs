using Diax.Domain.AI;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiUsageLogRepository : Repository<AiUsageLog>, IAiUsageLogRepository
{
    public AiUsageLogRepository(DiaxDbContext context) : base(context) { }

    public async Task<AiUsageSummary> GetSummaryAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? providerId,
        Guid? modelId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        // Apply filters
        if (startDate.HasValue)
            query = query.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(x => x.CreatedAt <= endDate.Value);
        if (providerId.HasValue)
            query = query.Where(x => x.ProviderId == providerId.Value);
        if (modelId.HasValue)
            query = query.Where(x => x.ModelId == modelId.Value);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        // Load with navigation properties
        var logs = await query
            .Include(x => x.Provider)
            .Include(x => x.Model)
            .ToListAsync(cancellationToken);

        // Aggregate in memory
        var byProvider = logs
            .GroupBy(x => x.Provider)
            .ToDictionary(
                g => g.Key.Name,
                g => new ProviderUsage(
                    g.Key.Id,
                    g.Key.Name,
                    g.Count(),
                    g.Sum(x => x.CostEstimated)
                )
            );

        return new AiUsageSummary(
            TotalRequests: logs.Count,
            TotalTokensInput: logs.Sum(x => x.TokensInput),
            TotalTokensOutput: logs.Sum(x => x.TokensOutput),
            TotalTokens: logs.Sum(x => x.TotalTokens),
            TotalCostEstimated: logs.Sum(x => x.CostEstimated),
            ByProvider: byProvider
        );
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }
}
