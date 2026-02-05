using Diax.Domain.AI;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiProviderRepository : Repository<AiProvider>, IAiProviderRepository
{
    public AiProviderRepository(DiaxDbContext context) : base(context) { }

    public async Task<AiProvider?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task<IEnumerable<AiProvider>> GetAllIncludedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Models)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
