using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class WealthProfileRepository : Repository<WealthProfile>, IWealthProfileRepository
{
    public WealthProfileRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<WealthProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }

    public new async Task AddAsync(WealthProfile profile, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(profile, cancellationToken);
    }

    public new Task UpdateAsync(WealthProfile profile, CancellationToken cancellationToken = default)
    {
        DbSet.Update(profile);
        return Task.CompletedTask;
    }
}
