using Diax.Domain.Outreach;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class OutreachConfigRepository : Repository<OutreachConfig>, IOutreachConfigRepository
{
    public OutreachConfigRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<OutreachConfig?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }
}
