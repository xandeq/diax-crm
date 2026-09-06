using Diax.Domain.Common;
using Diax.Domain.Customers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Application.Customers.Services;

/// <summary>
/// Checagem de MX em lote com cache persistente por domínio (EXTR-01 / D-03).
/// Ordem de decisão — porta de `deliverable_domain` em docs/email-marketing/mx_check.py:
///   1. domínio lixo/placeholder  → NoMx, custo zero (nenhuma query DNS)
///   2. cache fresco              → resultado cacheado, custo zero
///   3. resolução DNS real        → IMxLookupService, com paralelismo limitado
///   4. grava/atualiza o cache
/// </summary>
public interface ICachedMxCheckService
{
    /// <summary>
    /// Resolve um conjunto de domínios. A chave do dicionário devolvido é o domínio
    /// NORMALIZADO (MxCacheEntry.Normalize). Nunca lança.
    /// </summary>
    Task<IReadOnlyDictionary<string, MxCheckResult>> CheckManyAsync(
        IReadOnlyCollection<string> domains,
        CancellationToken cancellationToken = default);
}

public class CachedMxCheckService : ICachedMxCheckService
{
    private readonly IMxLookupService _lookup;
    private readonly IMxCacheRepository _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ExtractorPullOptions _options;
    private readonly ILogger<CachedMxCheckService> _logger;

    public CachedMxCheckService(
        IMxLookupService lookup,
        IMxCacheRepository cache,
        IUnitOfWork unitOfWork,
        IOptions<ExtractorPullOptions> options,
        ILogger<CachedMxCheckService> logger)
    {
        _lookup = lookup;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, MxCheckResult>> CheckManyAsync(
        IReadOnlyCollection<string> domains,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, MxCheckResult>(StringComparer.Ordinal);

        var normalized = domains
            .Select(MxCacheEntry.Normalize)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalized.Count == 0)
            return results;

        if (!_options.MxCheckEnabled)
        {
            foreach (var d in normalized)
                results[d] = MxCheckResult.Unverified;
            return results;
        }

        // 1. Domínio lixo: decisão sem I/O nenhum.
        var toResolve = new List<string>();
        foreach (var d in normalized)
        {
            if (JunkDomainFilter.IsJunk(d))
                results[d] = MxCheckResult.NoMx;
            else
                toResolve.Add(d);
        }

        if (toResolve.Count == 0)
            return results;

        // 2. Cache fresco.
        var cached = await _cache.GetByDomainsAsync(toResolve, cancellationToken);
        var now = DateTime.UtcNow;
        var stillToResolve = new List<string>();

        foreach (var d in toResolve)
        {
            if (cached.TryGetValue(d, out var entry) && entry.IsFresh(now, TtlFor(entry.ResultCode)))
                results[d] = (MxCheckResult)entry.ResultCode;
            else
                stillToResolve.Add(d);
        }

        if (stillToResolve.Count == 0)
            return results;

        // 3. Resolução DNS real, paralelismo limitado.
        var resolved = new System.Collections.Concurrent.ConcurrentDictionary<string, MxCheckResult>(StringComparer.Ordinal);
        var maxDop = Math.Max(1, _options.MxLookupParallelism);

        await Parallel.ForEachAsync(
            stillToResolve,
            new ParallelOptions { MaxDegreeOfParallelism = maxDop, CancellationToken = cancellationToken },
            async (domain, ct) =>
            {
                var r = await _lookup.CheckAsync(domain, ct);
                resolved[domain] = r;
            });

        // 4. Persiste o cache (inclusive Unverified — com TTL curto — para não martelar o mesmo
        //    domínio problemático a cada rodada).
        foreach (var (domain, result) in resolved)
        {
            results[domain] = result;

            if (cached.TryGetValue(domain, out var existing))
            {
                existing.Refresh((int)result);
                await _cache.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                await _cache.AddAsync(new MxCacheEntry(domain, (int)result), cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Checagem de MX: {Total} domínio(s) distinto(s) — {Junk} lixo, {Cached} de cache, {Resolved} resolvido(s) por DNS.",
            normalized.Count,
            normalized.Count - toResolve.Count,
            toResolve.Count - stillToResolve.Count,
            stillToResolve.Count);

        return results;
    }

    private TimeSpan TtlFor(int resultCode) =>
        resultCode == (int)MxCheckResult.Unverified
            ? TimeSpan.FromHours(Math.Max(1, _options.MxUnverifiedCacheHours))
            : TimeSpan.FromDays(Math.Max(1, _options.MxCacheDays));
}
