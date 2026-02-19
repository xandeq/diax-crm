using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class EmailQueueRepository : Repository<EmailQueueItem>, IEmailQueueRepository
{
    public EmailQueueRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EmailQueueItem>> GetPendingBatchAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return [];
        }

        return await DbSet
            .Where(item => item.Status == EmailQueueStatus.Queued && item.ScheduledAt <= utcNow)
            .OrderBy(item => item.ScheduledAt)
            .ThenBy(item => item.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<EmailQueueItem> items, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(items, cancellationToken);
    }

    public async Task<int> CountSentSinceAsync(DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(
                item => item.Status == EmailQueueStatus.Sent
                    && item.SentAt.HasValue
                    && item.SentAt.Value >= fromUtc,
                cancellationToken);
    }

    public async Task<(IEnumerable<EmailQueueItem> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(item => item.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
