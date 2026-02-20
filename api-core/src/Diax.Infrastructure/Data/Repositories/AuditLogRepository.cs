using Diax.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

/// <summary>
/// Repositório de logs de auditoria. Operações são somente leitura/escrita simples;
/// a criação automática é feita pelo AuditSaveChangesInterceptor.
/// </summary>
public class AuditLogRepository : Repository<AuditLogEntry>, IAuditLogRepository
{
    public AuditLogRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entry, cancellationToken);
    }

    public async Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetFilteredAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(e => e.UserId == filter.UserId);

        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
            query = query.Where(e => e.ResourceType == filter.ResourceType);

        if (!string.IsNullOrWhiteSpace(filter.ResourceId))
            query = query.Where(e => e.ResourceId == filter.ResourceId);

        if (filter.Action.HasValue)
            query = query.Where(e => e.Action == filter.Action);

        if (filter.Source.HasValue)
            query = query.Where(e => e.Source == filter.Source);

        if (filter.Status.HasValue)
            query = query.Where(e => e.Status == filter.Status);

        if (filter.FromDate.HasValue)
            query = query.Where(e => e.TimestampUtc >= filter.FromDate);

        if (filter.ToDate.HasValue)
            query = query.Where(e => e.TimestampUtc <= filter.ToDate);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.ToLower();
            query = query.Where(e =>
                e.Description.ToLower().Contains(search) ||
                e.ResourceId.ToLower().Contains(search) ||
                e.ResourceType.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.TimestampUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<AuditLogEntry>> GetByResourceAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(e => e.ResourceType == resourceType && e.ResourceId == resourceId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLogEntry>> GetByUserAsync(
        Guid userId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLogEntry> query = DbSet
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.TimestampUtc);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.TimestampUtc < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
