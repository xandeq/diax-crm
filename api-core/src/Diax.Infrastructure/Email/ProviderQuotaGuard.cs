using System.Collections.Concurrent;
using Diax.Application.EmailMarketing.Dispatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Controla envios diários e semanais por provider para nunca exceder limites.
/// Singleton — estado em memória, thread-safe.
/// Diário reseta à meia-noite UTC; semanal na segunda-feira 00:00 UTC.
///
/// No primeiro consumo de cada período o contador é HIDRATADO do banco
/// (IProviderQuotaUsageSource) — sem isso, um recycle do app pool zerava a quota
/// no meio do dia e os limites externos eram silenciosamente ultrapassados.
/// </summary>
public sealed class ProviderQuotaGuard : IProviderQuotaGuard
{
    private sealed class PeriodBucket
    {
        public int Used;
        public bool Hydrated;
        public DateTime PeriodStartUtc;
        public readonly SemaphoreSlim Gate = new(1, 1);
    }

    private readonly IOptionsMonitor<EmailChainOptions> _options;
    private readonly IProviderQuotaUsageSource _usageSource;
    private readonly ILogger<ProviderQuotaGuard> _logger;

    // Buckets separados por provider para evitar contenção cruzada
    private readonly ConcurrentDictionary<string, PeriodBucket> _daily  = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PeriodBucket> _weekly = new(StringComparer.OrdinalIgnoreCase);

    public ProviderQuotaGuard(
        IOptionsMonitor<EmailChainOptions> options,
        IProviderQuotaUsageSource usageSource,
        ILogger<ProviderQuotaGuard> logger)
    {
        _options = options;
        _usageSource = usageSource;
        _logger = logger;
    }

    public async Task<bool> TryConsumeAsync(string providerKey, CancellationToken ct = default)
    {
        providerKey = providerKey.ToLowerInvariant();
        var opts = _options.CurrentValue;

        var dailyLimit  = GetLimit(opts.ProviderDailyLimits,  providerKey);
        var weeklyLimit = GetLimit(opts.ProviderWeeklyLimits, providerKey);

        PeriodBucket? db = null, wb = null;

        // Adquire os dois gates em ordem fixa (semanal → diário) para evitar deadlock,
        // e só incrementa depois que AMBOS os limites passaram — fecha a janela
        // check-then-increment da versão anterior.
        if (weeklyLimit > 0)
        {
            wb = _weekly.GetOrAdd(providerKey, _ => new PeriodBucket { PeriodStartUtc = CurrentWeekStartUtc() });
            await wb.Gate.WaitAsync(ct);
        }

        try
        {
            if (dailyLimit > 0)
            {
                db = _daily.GetOrAdd(providerKey, _ => new PeriodBucket { PeriodStartUtc = DateTime.UtcNow.Date });
                await db.Gate.WaitAsync(ct);
            }

            try
            {
                if (wb is not null)
                {
                    MaybeResetWeekly(wb);
                    await HydrateAsync(wb, providerKey, CurrentWeekStartUtc(), ct);
                    if (wb.Used >= weeklyLimit)
                    {
                        _logger.LogWarning("Provider {Provider} quota SEMANAL esgotada ({Used}/{Limit}) — reset na próxima segunda {Reset:yyyy-MM-dd} UTC",
                            providerKey, wb.Used, weeklyLimit, NextWeekStartUtc());
                        return false;
                    }
                }

                if (db is not null)
                {
                    MaybeResetDaily(db);
                    await HydrateAsync(db, providerKey, DateTime.UtcNow.Date, ct);
                    if (db.Used >= dailyLimit)
                    {
                        _logger.LogWarning("Provider {Provider} quota DIÁRIA esgotada ({Used}/{Limit}) — reset às {Reset:HH:mm} UTC",
                            providerKey, db.Used, dailyLimit, NextMidnightUtc());
                        return false;
                    }
                }

                // Ambos OK — incrementa com os gates ainda adquiridos.
                if (wb is not null) wb.Used++;
                if (db is not null) db.Used++;
                return true;
            }
            finally
            {
                db?.Gate.Release();
            }
        }
        finally
        {
            wb?.Gate.Release();
        }
    }

    public async Task<int> GetRemainingAsync(string providerKey, CancellationToken ct = default)
    {
        providerKey = providerKey.ToLowerInvariant();
        var opts = _options.CurrentValue;

        var dailyLimit  = GetLimit(opts.ProviderDailyLimits,  providerKey);
        var weeklyLimit = GetLimit(opts.ProviderWeeklyLimits, providerKey);

        var dailyRemaining = dailyLimit > 0
            ? await RemainingInBucketAsync(_daily, providerKey, dailyLimit, DateTime.UtcNow.Date, MaybeResetDaily, ct)
            : int.MaxValue;
        var weeklyRemaining = weeklyLimit > 0
            ? await RemainingInBucketAsync(_weekly, providerKey, weeklyLimit, CurrentWeekStartUtc(), MaybeResetWeekly, ct)
            : int.MaxValue;

        return Math.Min(dailyRemaining, weeklyRemaining);
    }

    public async Task<IReadOnlyDictionary<string, ProviderQuotaStatus>> GetStatusAsync(CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;
        var result = new Dictionary<string, ProviderQuotaStatus>(StringComparer.OrdinalIgnoreCase);

        // Union de providers configurados em qualquer limite
        var providers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in opts.ProviderDailyLimits.Keys)  providers.Add(k.ToLowerInvariant());
        foreach (var k in opts.ProviderWeeklyLimits.Keys) providers.Add(k.ToLowerInvariant());

        var dailyReset  = NextMidnightUtc();
        var weeklyReset = NextWeekStartUtc();

        foreach (var key in providers)
        {
            var dl = GetLimit(opts.ProviderDailyLimits,  key);
            var wl = GetLimit(opts.ProviderWeeklyLimits, key);

            int du = 0, wu = 0;

            if (dl > 0)
                du = await UsedInBucketAsync(_daily, key, DateTime.UtcNow.Date, MaybeResetDaily, ct);

            if (wl > 0)
                wu = await UsedInBucketAsync(_weekly, key, CurrentWeekStartUtc(), MaybeResetWeekly, ct);

            result[key] = new ProviderQuotaStatus(
                Provider: key,
                DailyUsed: du,
                DailyLimit: dl,
                DailyRemaining: dl > 0 ? Math.Max(0, dl - du) : int.MaxValue,
                DailyResetAtUtc: dailyReset,
                WeeklyUsed: wu,
                WeeklyLimit: wl,
                WeeklyRemaining: wl > 0 ? Math.Max(0, wl - wu) : int.MaxValue,
                WeeklyResetAtUtc: weeklyReset
            );
        }

        return result;
    }

    // ───── helpers ─────

    private async Task HydrateAsync(PeriodBucket bucket, string providerKey, DateTime periodStart, CancellationToken ct)
    {
        if (bucket.Hydrated)
            return;

        try
        {
            var used = await _usageSource.GetUsedSinceAsync(providerKey, periodStart, ct);
            // max(): não perde incrementos in-memory que ainda não chegaram ao banco.
            bucket.Used = Math.Max(bucket.Used, used);
            _logger.LogInformation(
                "Quota {Provider} hidratada do banco: {Used} envios desde {Start:u}",
                providerKey, bucket.Used, periodStart);
        }
        catch (Exception ex)
        {
            // Falha de hidratação não pode derrubar o envio — segue com contador in-memory
            // (comportamento antigo) e tenta de novo no próximo consumo.
            _logger.LogError(ex, "Falha ao hidratar quota do provider {Provider} — usando contador in-memory", providerKey);
            return;
        }

        bucket.Hydrated = true;
    }

    private async Task<int> RemainingInBucketAsync(
        ConcurrentDictionary<string, PeriodBucket> dict,
        string key,
        int limit,
        DateTime periodStart,
        Action<PeriodBucket> resetFn,
        CancellationToken ct)
    {
        var used = await UsedInBucketAsync(dict, key, periodStart, resetFn, ct);
        return Math.Max(0, limit - used);
    }

    private async Task<int> UsedInBucketAsync(
        ConcurrentDictionary<string, PeriodBucket> dict,
        string key,
        DateTime periodStart,
        Action<PeriodBucket> resetFn,
        CancellationToken ct)
    {
        var bucket = dict.GetOrAdd(key, _ => new PeriodBucket { PeriodStartUtc = periodStart });
        await bucket.Gate.WaitAsync(ct);
        try
        {
            resetFn(bucket);
            await HydrateAsync(bucket, key, periodStart, ct);
            return bucket.Used;
        }
        finally
        {
            bucket.Gate.Release();
        }
    }

    private static int GetLimit(Dictionary<string, int> dict, string key) =>
        dict.TryGetValue(key, out var v) ? v : 0;

    private static void MaybeResetDaily(PeriodBucket b)
    {
        var today = DateTime.UtcNow.Date;
        if (b.PeriodStartUtc == today) return;
        b.Used = 0;
        b.Hydrated = false;
        b.PeriodStartUtc = today;
    }

    private static void MaybeResetWeekly(PeriodBucket b)
    {
        var thisWeek = CurrentWeekStartUtc();
        if (b.PeriodStartUtc == thisWeek) return;
        b.Used = 0;
        b.Hydrated = false;
        b.PeriodStartUtc = thisWeek;
    }

    private static DateTime CurrentWeekStartUtc()
    {
        var today = DateTime.UtcNow.Date;
        // DayOfWeek.Monday = 1; move back to last Monday
        int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }

    private static DateTime NextMidnightUtc() => DateTime.UtcNow.Date.AddDays(1);

    private static DateTime NextWeekStartUtc() => CurrentWeekStartUtc().AddDays(7);
}
