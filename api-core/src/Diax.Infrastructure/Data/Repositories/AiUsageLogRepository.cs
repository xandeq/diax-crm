using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiUsageLogRepository : Repository<AiUsageLog>, IAiUsageLogRepository
{
    public AiUsageLogRepository(DiaxDbContext context) : base(context) { }

    public async Task<AiUsageLog> CreateAsync(AiUsageLog log, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(log, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<List<AiUsageLog>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.Model)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AiUsageLog>> GetByProviderIdAsync(Guid providerId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.Model)
            .Where(x => x.ProviderId == providerId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AiUsageLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.Model)
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int totalLogs, int totalTokens, decimal totalCost)> GetUsageStatsAsync(
        Guid? userId = null,
        Guid? providerId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (providerId.HasValue)
            query = query.Where(x => x.ProviderId == providerId.Value);

        if (startDate.HasValue)
            query = query.Where(x => x.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.CreatedAt <= endDate.Value);

        var logs = await query
            .Where(x => x.Success) // Only count successful requests
            .ToListAsync(cancellationToken);

        var totalLogs = logs.Count;
        var totalTokens = logs.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0));
        var totalCost = logs.Sum(x => x.EstimatedCost ?? 0);

        return (totalLogs, totalTokens, totalCost);
    }

    public async Task<List<(Guid ProviderId, string ProviderName, int TotalLogs, int TotalTokens, decimal TotalCost)>> GetGroupedByProviderStatsAsync(
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (startDate.HasValue)
            query = query.Where(x => x.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.CreatedAt <= endDate.Value);

        var result = await query
            .Where(x => x.Success)
            .GroupBy(x => new { x.ProviderId, x.Provider.Name })
            .Select(g => new
            {
                ProviderId = g.Key.ProviderId,
                ProviderName = g.Key.Name,
                TotalLogs = g.Count(),
                TotalTokens = g.Sum(x => (x.InputTokens ?? 0) + (x.OutputTokens ?? 0)),
                TotalCost = g.Sum(x => x.EstimatedCost ?? 0)
            })
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.ProviderId, x.ProviderName, x.TotalLogs, x.TotalTokens, x.TotalCost)).ToList();
    }
}
