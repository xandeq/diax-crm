using Diax.Domain.AI;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiModelRepository : Repository<AiModel>, IAiModelRepository
{
    public AiModelRepository(DiaxDbContext context) : base(context) { }

    public async Task<IEnumerable<AiModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ProviderId == providerId)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AiModel>> GetEnabledByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ProviderId == providerId && x.IsEnabled)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiModel?> GetByProviderAndModelKeyAsync(Guid providerId, string modelKey, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.ModelKey == modelKey, cancellationToken);
    }
}
