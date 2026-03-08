using Diax.Domain.Logs;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AppLogRepository : Repository<AppLog>, IAppLogRepository
{
    public AppLogRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<AppLog> Items, int TotalCount)> GetFilteredAsync(
        DateTime? fromDate,
        DateTime? toDate,
        LogLevel? level,
        LogCategory? category,
        string? search,
        string? userId,
        string? correlationId,
        string? requestId,
        string? path,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(x => x.TimestampUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.TimestampUtc <= toDate.Value);

        if (level.HasValue)
            query = query.Where(x => x.Level == level.Value);

        if (category.HasValue)
            query = query.Where(x => x.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Message.Contains(search) ||
                                     (x.ExceptionMessage != null && x.ExceptionMessage.Contains(search)));

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);

        if (!string.IsNullOrWhiteSpace(requestId))
            query = query.Where(x => x.RequestId == requestId);

        if (!string.IsNullOrWhiteSpace(path))
            query = query.Where(x => x.RequestPath != null && x.RequestPath.Contains(path));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Dictionary<LogLevel, int>> GetStatsByLevelAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(x => x.TimestampUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.TimestampUtc <= toDate.Value);

        var stats = await query
            .GroupBy(x => x.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return stats.ToDictionary(x => x.Level, x => x.Count);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.TimestampUtc < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ExecuteDeleteAsync(cancellationToken);
    }
}
