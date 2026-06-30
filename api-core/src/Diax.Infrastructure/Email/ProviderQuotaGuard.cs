using System.Collections.Concurrent;
using Diax.Application.EmailMarketing.Dispatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Controla envios diários e semanais por provider para nunca exceder limites.
/// Singleton — estado em memória, thread-safe.
/// Diário reseta à meia-noite UTC; semanal na segunda-feira 00:00 UTC.
/// </summary>
public sealed class ProviderQuotaGuard : IProviderQuotaGuard
{
    private sealed class PeriodBucket
    {
        public volatile int Used;
        public DateTime PeriodStartUtc;
        public readonly object Lock = new();
    }

    private readonly IOptionsMonitor<EmailChainOptions> _options;
    private readonly ILogger<ProviderQuotaGuard> _logger;

    // Buckets separados por provider para evitar contenção cruzada
    private readonly ConcurrentDictionary<string, PeriodBucket> _daily  = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PeriodBucket> _weekly = new(StringComparer.OrdinalIgnoreCase);

    public ProviderQuotaGuard(IOptionsMonitor<EmailChainOptions> options, ILogger<ProviderQuotaGuard> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool TryConsume(string providerKey)
    {
        providerKey = providerKey.ToLowerInvariant();
        var opts = _options.CurrentValue;

        var dailyLimit  = GetLimit(opts.ProviderDailyLimits,  providerKey);
        var weeklyLimit = GetLimit(opts.ProviderWeeklyLimits, providerKey);

        // Verifica semanal primeiro (mais raro de atingir, mas importante)
        if (weeklyLimit > 0)
        {
            var wb = _weekly.GetOrAdd(providerKey, _ => new PeriodBucket { PeriodStartUtc = CurrentWeekStartUtc() });
            lock (wb.Lock)
            {
                MaybeResetWeekly(wb);
                if (wb.Used >= weeklyLimit)
                {
                    _logger.LogWarning("Provider {Provider} quota SEMANAL esgotada ({Used}/{Limit}) — reset na próxima segunda {Reset:yyyy-MM-dd} UTC",
                        providerKey, wb.Used, weeklyLimit, NextWeekStartUtc());
                    return false;
                }
            }
        }

        // Verifica diário
        if (dailyLimit > 0)
        {
            var db = _daily.GetOrAdd(providerKey, _ => new PeriodBucket { PeriodStartUtc = DateTime.UtcNow.Date });
            lock (db.Lock)
            {
                MaybeResetDaily(db);
                if (db.Used >= dailyLimit)
                {
                    _logger.LogWarning("Provider {Provider} quota DIÁRIA esgotada ({Used}/{Limit}) — reset às {Reset:HH:mm} UTC",
                        providerKey, db.Used, dailyLimit, NextMidnightUtc());
                    return false;
                }
            }
        }

        // Ambos OK — incrementa
        if (dailyLimit > 0)
        {
            var db = _daily[providerKey];
            lock (db.Lock) { db.Used++; }
        }
        if (weeklyLimit > 0)
        {
            var wb = _weekly[providerKey];
            lock (wb.Lock) { wb.Used++; }
        }

        return true;
    }

    public int GetRemaining(string providerKey)
    {
        providerKey = providerKey.ToLowerInvariant();
        var opts = _options.CurrentValue;

        var dailyLimit  = GetLimit(opts.ProviderDailyLimits,  providerKey);
        var weeklyLimit = GetLimit(opts.ProviderWeeklyLimits, providerKey);

        int dailyRemaining  = dailyLimit  > 0 ? RemainingInBucket(_daily,  providerKey, dailyLimit,  MaybeResetDaily)  : int.MaxValue;
        int weeklyRemaining = weeklyLimit > 0 ? RemainingInBucket(_weekly, providerKey, weeklyLimit, MaybeResetWeekly) : int.MaxValue;

        return Math.Min(dailyRemaining, weeklyRemaining);
    }

    public IReadOnlyDictionary<string, ProviderQuotaStatus> GetStatus()
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

            if (dl > 0 && _daily.TryGetValue(key, out var db))
                lock (db.Lock) { MaybeResetDaily(db); du = db.Used; }

            if (wl > 0 && _weekly.TryGetValue(key, out var wb))
                lock (wb.Lock) { MaybeResetWeekly(wb); wu = wb.Used; }

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

    private static int GetLimit(Dictionary<string, int> dict, string key) =>
        dict.TryGetValue(key, out var v) ? v : 0;

    private static int RemainingInBucket(
        ConcurrentDictionary<string, PeriodBucket> dict,
        string key,
        int limit,
        Action<PeriodBucket> resetFn)
    {
        if (!dict.TryGetValue(key, out var b)) return limit;
        lock (b.Lock) { resetFn(b); return Math.Max(0, limit - b.Used); }
    }

    private static void MaybeResetDaily(PeriodBucket b)
    {
        var today = DateTime.UtcNow.Date;
        if (b.PeriodStartUtc == today) return;
        b.Used = 0;
        b.PeriodStartUtc = today;
    }

    private static void MaybeResetWeekly(PeriodBucket b)
    {
        var thisWeek = CurrentWeekStartUtc();
        if (b.PeriodStartUtc == thisWeek) return;
        b.Used = 0;
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
