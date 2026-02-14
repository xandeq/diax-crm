using Diax.Domain.ApiKeys;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório de ApiKey.
/// </summary>
public class ApiKeyRepository : Repository<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<ApiKey?> GetByKeyHashAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetActiveKeysAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsEnabled && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > DateTime.UtcNow))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
