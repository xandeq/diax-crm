using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class MeetingRepository : Repository<Meeting>, IMeetingRepository
{
    public MeetingRepository(DiaxDbContext context) : base(context) { }

    public async Task<List<Meeting>> GetConfirmedInRangeAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.UserId == userId
                && m.Status == MeetingStatus.Confirmed
                && m.ScheduledAt >= fromUtc
                && m.ScheduledAt < toUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Meeting>> GetUpcomingAsync(
        Guid userId, int take = 50, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.UserId == userId
                && m.Status == MeetingStatus.Confirmed
                && m.ScheduledAt >= DateTime.UtcNow)
            .OrderBy(m => m.ScheduledAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);
    }
}
