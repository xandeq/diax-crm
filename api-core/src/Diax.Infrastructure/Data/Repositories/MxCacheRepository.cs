using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

/// <summary>Implementação EF do cache persistente de MX.</summary>
public class MxCacheRepository : Repository<MxCacheEntry>, IMxCacheRepository
{
    public MxCacheRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<MxCacheEntry?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        var d = MxCacheEntry.Normalize(domain);
        return await DbSet.FirstOrDefaultAsync(x => x.Domain == d, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, MxCacheEntry>> GetByDomainsAsync(
        IReadOnlyCollection<string> domains,
        CancellationToken cancellationToken = default)
    {
        var normalized = domains.Select(MxCacheEntry.Normalize)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
            return new Dictionary<string, MxCacheEntry>();

        var entries = await DbSet
            .Where(x => normalized.Contains(x.Domain))
            .ToListAsync(cancellationToken);

        return entries.ToDictionary(e => e.Domain, e => e);
    }
}
