using System.Collections.Concurrent;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Circuit breaker por provider, thread-safe. Singleton — mantém estado em memória.
///
/// Estados: Closed → Open (cooldown de 5 min) → Half-Open (1 tentativa de prova)
///   → Closed (prova ok) ou Open novamente (prova falhou, novo cooldown).
///
/// Sem o half-open o breaker era *latching*: uma vez aberto só fechava com restart do
/// processo — um provider saudável ficava fora da rotação para sempre.
/// </summary>
public class EmailProviderCircuitBreaker : IProviderCircuitBreaker
{
    private const int WindowSize = 10;
    private const double MaxErrorRatePercent = 30.0;
    private static readonly TimeSpan DefaultOpenCooldown = TimeSpan.FromMinutes(5);

    private readonly TimeSpan _openCooldown;

    /// <summary>Ctor padrão (DI) — cooldown de 5 minutos.</summary>
    public EmailProviderCircuitBreaker() : this(DefaultOpenCooldown)
    {
    }

    /// <summary>Cooldown configurável (testes).</summary>
    public EmailProviderCircuitBreaker(TimeSpan openCooldown)
    {
        _openCooldown = openCooldown;
    }

    private sealed class BreakerState
    {
        public bool IsOpen;
        public string? Reason;
        public DateTime? OpenedAtUtc;
        public bool ProbeInFlight;
        public readonly Queue<bool> Window = new();
        public readonly object Lock = new();
    }

    private readonly ConcurrentDictionary<string, BreakerState> _states = new(StringComparer.OrdinalIgnoreCase);

    private BreakerState GetOrCreate(string key) =>
        _states.GetOrAdd(key, _ => new BreakerState());

    public bool IsOpen(string providerKey)
    {
        if (!_states.TryGetValue(providerKey, out var s))
            return false;

        lock (s.Lock)
        {
            if (!s.IsOpen)
                return false;

            var elapsed = DateTime.UtcNow - (s.OpenedAtUtc ?? DateTime.UtcNow);
            if (elapsed < _openCooldown)
                return true;

            // Half-open: libera exatamente UMA prova; as demais chamadas seguem bloqueadas
            // até a prova resolver (RecordSuccess fecha, RecordFailure reabre).
            if (s.ProbeInFlight)
                return true;

            s.ProbeInFlight = true;
            return false;
        }
    }

    public void RecordSuccess(string providerKey)
    {
        var s = GetOrCreate(providerKey);
        lock (s.Lock)
        {
            s.Window.Enqueue(true);
            Trim(s.Window);

            if (s.IsOpen || s.ProbeInFlight)
            {
                // Prova do half-open (ou reset natural) bem-sucedida: fecha e zera a janela
                // para que falhas antigas não reabram o circuito no próximo erro isolado.
                s.IsOpen = false;
                s.Reason = null;
                s.OpenedAtUtc = null;
                s.ProbeInFlight = false;
                s.Window.Clear();
                s.Window.Enqueue(true);
            }
        }
    }

    public void RecordFailure(string providerKey, string? errorMessage)
    {
        var s = GetOrCreate(providerKey);
        lock (s.Lock)
        {
            var wasProbe = s.ProbeInFlight;
            s.ProbeInFlight = false;

            if (EmailErrorClassifier.IsCriticalAuthError(errorMessage))
            {
                s.Window.Enqueue(false);
                Trim(s.Window);
                Open(s, $"Erro crítico de autenticação: {errorMessage}");
                return;
            }

            if (EmailErrorClassifier.IsIgnorable(errorMessage))
            {
                // Rate-limit puro é transitório — mas se era a prova do half-open,
                // reabre para não liberar tráfego num provider ainda saturado.
                if (wasProbe)
                    Open(s, "Prova half-open recebeu rate-limit");
                return;
            }

            s.Window.Enqueue(false);
            Trim(s.Window);

            if (wasProbe)
            {
                Open(s, $"Prova half-open falhou: {errorMessage}");
                return;
            }

            if (s.Window.Count >= 3)
            {
                var failures = s.Window.Count(x => !x);
                var rate = (double)failures / s.Window.Count * 100.0;
                if (rate >= MaxErrorRatePercent)
                {
                    Open(s, $"Taxa de erro: {rate:F1}%");
                }
            }
        }
    }

    public void Reset(string providerKey)
    {
        if (!_states.TryGetValue(providerKey, out var s))
            return;

        lock (s.Lock)
        {
            s.IsOpen = false;
            s.Reason = null;
            s.OpenedAtUtc = null;
            s.ProbeInFlight = false;
            s.Window.Clear();
        }
    }

    public IReadOnlyDictionary<string, ProviderBreakerStatus> GetStates()
    {
        var result = new Dictionary<string, ProviderBreakerStatus>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, s) in _states)
        {
            lock (s.Lock)
            {
                var errorRate = s.Window.Count == 0
                    ? 0.0
                    : (double)s.Window.Count(x => !x) / s.Window.Count * 100.0;

                var isHalfOpen = s.IsOpen
                    && s.OpenedAtUtc.HasValue
                    && DateTime.UtcNow - s.OpenedAtUtc.Value >= _openCooldown;

                result[key] = new ProviderBreakerStatus(
                    Provider: key,
                    IsOpen: s.IsOpen,
                    IsHalfOpen: isHalfOpen,
                    Reason: s.Reason,
                    OpenedAtUtc: s.OpenedAtUtc,
                    ErrorRatePercent: errorRate);
            }
        }

        return result;
    }

    private static void Open(BreakerState s, string reason)
    {
        s.IsOpen = true;
        s.Reason = reason;
        s.OpenedAtUtc = DateTime.UtcNow;
    }

    private static void Trim(Queue<bool> q)
    {
        while (q.Count > WindowSize)
            q.Dequeue();
    }
}
