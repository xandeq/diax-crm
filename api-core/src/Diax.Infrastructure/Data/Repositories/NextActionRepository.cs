using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class NextActionRepository : Repository<NextAction>, INextActionRepository
{
    public NextActionRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<List<NextAction>> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId && x.Status == NextAction.StatusPending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> HasAnyOnOrAfterAsync(Guid userId, DateTime sinceUtc, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(x => x.UserId == userId && x.CreatedAt >= sinceUtc, ct);
    }

    public async Task<NextAction?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }

    public async Task AddRangeAsync(IEnumerable<NextAction> actions, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(actions, cancellationToken);
    }

    public new Task UpdateAsync(NextAction action, CancellationToken cancellationToken = default)
    {
        DbSet.Update(action);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<NextAction> actions, CancellationToken cancellationToken = default)
    {
        DbSet.RemoveRange(actions);
        return Task.CompletedTask;
    }
}
