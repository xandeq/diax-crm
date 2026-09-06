using Diax.Domain.Common;

namespace Diax.Domain.Customers;

/// <summary>Repositório do cache persistente de MX por domínio (EXTR-01 / D-03).</summary>
public interface IMxCacheRepository : IRepository<MxCacheEntry>
{
    /// <summary>Busca a entrada pelo domínio já normalizado (MxCacheEntry.Normalize). Null se não existe.</summary>
    Task<MxCacheEntry?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>Carrega de uma vez todas as entradas dos domínios informados (evita N+1 no import em lote).</summary>
    Task<IReadOnlyDictionary<string, MxCacheEntry>> GetByDomainsAsync(
        IReadOnlyCollection<string> domains,
        CancellationToken cancellationToken = default);
}
