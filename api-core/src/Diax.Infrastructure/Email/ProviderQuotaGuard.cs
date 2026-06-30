using System.Collections.Concurrent;
using Diax.Application.EmailMarketing.Dispatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Controla o número de envios diários por provider para nunca estourar limites.
/// Singleton — estado compartilhado em memória, reseta à meia-noite UTC.
/// Thread-safe via Interlocked + objeto de lock por provider.
/// </summary>
public sealed class ProviderQuotaGuard : IProviderQuotaGuard
{
    private sealed class QuotaState
    {
        public volatile int Used;
        public DateTime DayUtc; // dia atual (só a data, sem hora)
        public readonly object Lock = new();
    }

    private readonly IOptionsMonitor<EmailChainOptions> _options;
    private readonly ILogger<ProviderQuotaGuard> _logger;
    private readonly ConcurrentDictionary<string, QuotaState> _states = new(StringComparer.OrdinalIgnoreCase);

    public ProviderQuotaGuard(IOptionsMonitor<EmailChainOptions> options, ILogger<ProviderQuotaGuard> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool TryConsume(string providerKey)
    {
        providerKey = providerKey.ToLowerInvariant();
        var limit = GetLimit(providerKey);
        if (limit <= 0) return true; // sem limite configurado = sempre livre

        var state = _states.GetOrAdd(providerKey, _ => new QuotaState { DayUtc = DateTime.UtcNow.Date });

        lock (state.Lock)
        {
            MaybeReset(state);

            if (state.Used >= limit)
            {
                _logger.LogWarning("Provider {Provider} quota diária esgotada ({Used}/{Limit}) — aguardando reset às {Reset:HH:mm} UTC",
                    providerKey, state.Used, limit, NextMidnightUtc());
                return false;
            }

            state.Used++;
            return true;
        }
    }

    public int GetRemaining(string providerKey)
    {
        providerKey = providerKey.ToLowerInvariant();
        var limit = GetLimit(providerKey);
        if (limit <= 0) return int.MaxValue;

        if (!_states.TryGetValue(providerKey, out var state)) return limit;

        lock (state.Lock)
        {
            MaybeReset(state);
            return Math.Max(0, limit - state.Used);
        }
    }

    public IReadOnlyDictionary<string, ProviderQuotaStatus> GetStatus()
    {
        var options = _options.CurrentValue;
        var result = new Dictionary<string, ProviderQuotaStatus>(StringComparer.OrdinalIgnoreCase);
        var resetAt = NextMidnightUtc();

        foreach (var (key, limit) in options.ProviderDailyLimits)
        {
            int used = 0;
            if (_states.TryGetValue(key, out var state))
            {
                lock (state.Lock)
                {
                    MaybeReset(state);
                    used = state.Used;
                }
            }
            result[key] = new ProviderQuotaStatus(key, used, limit, Math.Max(0, limit - used), resetAt);
        }

        return result;
    }

    private int GetLimit(string providerKey)
    {
        var limits = _options.CurrentValue.ProviderDailyLimits;
        return limits.TryGetValue(providerKey, out var l) ? l : 0;
    }

    private static void MaybeReset(QuotaState state)
    {
        var today = DateTime.UtcNow.Date;
        if (state.DayUtc == today) return;
        state.Used = 0;
        state.DayUtc = today;
    }

    private static DateTime NextMidnightUtc()
    {
        var now = DateTime.UtcNow;
        return now.Date.AddDays(1);
    }
}
