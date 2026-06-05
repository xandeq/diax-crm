using Diax.Domain.ErrorLogs;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ErrorLogRepository : IErrorLogRepository
{
    private readonly DiaxDbContext _db;

    public ErrorLogRepository(DiaxDbContext db) => _db = db;

    public async Task AddAsync(ErrorLog log, CancellationToken ct = default)
        => await _db.ErrorLogs.AddAsync(log, ct);

    public async Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ErrorLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<ErrorLog?> GetOpenByFingerprintAsync(string fingerprint, string appName, CancellationToken ct = default)
        => await _db.ErrorLogs
            .Where(x => x.Fingerprint == fingerprint && x.AppName == appName && !x.IsResolved)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<ErrorLog> Items, int TotalCount)> GetFilteredAsync(
        ErrorLogFilter filter, CancellationToken ct = default)
    {
        var query = _db.ErrorLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.AppName))
            query = query.Where(x => x.AppName == filter.AppName);

        if (filter.Level.HasValue)
            query = query.Where(x => x.Level == filter.Level.Value);

        if (filter.IsResolved.HasValue)
            query = query.Where(x => x.IsResolved == filter.IsResolved.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.OccurredAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.OccurredAt <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search;
            query = query.Where(x =>
                x.Message.Contains(s) ||
                (x.ExceptionType != null && x.ExceptionType.Contains(s)) ||
                (x.Source != null && x.Source.Contains(s)));
        }

        // Cursor-based: Id > cursor (cursor = last seen Id as string)
        if (!string.IsNullOrWhiteSpace(filter.Cursor) && Guid.TryParse(filter.Cursor, out var cursorId))
            query = query.Where(x => x.Id != cursorId); // simplificado: offset by CreatedAt

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(Math.Min(filter.Limit, 100))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<ErrorLogStats> GetStatsAsync(CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var totalToday = await _db.ErrorLogs.CountAsync(x => x.OccurredAt >= todayUtc, ct);
        var criticalToday = await _db.ErrorLogs.CountAsync(
            x => x.OccurredAt >= todayUtc && x.Level == ErrorLogLevel.Critical, ct);
        var unresolvedTotal = await _db.ErrorLogs.CountAsync(x => !x.IsResolved, ct);

        var byApp = await _db.ErrorLogs
            .Where(x => !x.IsResolved)
            .GroupBy(x => x.AppName)
            .Select(g => new AppErrorCount(g.Key, g.Count()))
            .ToListAsync(ct);

        return new ErrorLogStats(totalToday, criticalToday, unresolvedTotal, byApp);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var deleted = 0;
        const int batchSize = 500;
        while (true)
        {
            var batch = await _db.ErrorLogs
                .Where(x => x.OccurredAt < cutoff)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;
            _db.ErrorLogs.RemoveRange(batch);
            await _db.SaveChangesAsync(ct);
            deleted += batch.Count;
            if (batch.Count < batchSize) break;
        }
        return deleted;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
