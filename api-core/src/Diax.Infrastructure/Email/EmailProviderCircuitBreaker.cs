using System.Collections.Concurrent;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Circuit breaker por provider, thread-safe. Singleton — mantém estado em memória.
/// Mesmo algoritmo do PilotCircuitBreaker mas indexado por chave DI do provider.
/// </summary>
public class EmailProviderCircuitBreaker : IProviderCircuitBreaker
{
    private const int WindowSize = 10;
    private const double MaxErrorRatePercent = 30.0;

    private sealed class BreakerState
    {
        public bool IsOpen;
        public string? Reason;
        public readonly Queue<bool> Window = new();
        public readonly object Lock = new();
    }

    private readonly ConcurrentDictionary<string, BreakerState> _states = new();

    private BreakerState GetOrCreate(string key) =>
        _states.GetOrAdd(key, _ => new BreakerState());

    public bool IsOpen(string providerKey) =>
        _states.TryGetValue(providerKey, out var s) && s.IsOpen;

    public void RecordSuccess(string providerKey)
    {
        var s = GetOrCreate(providerKey);
        lock (s.Lock)
        {
            s.Window.Enqueue(true);
            Trim(s.Window);
        }
    }

    public void RecordFailure(string providerKey, string? errorMessage)
    {
        var s = GetOrCreate(providerKey);
        lock (s.Lock)
        {
            if (EmailErrorClassifier.IsCriticalAuthError(errorMessage))
            {
                s.Window.Enqueue(false);
                Trim(s.Window);
                s.IsOpen = true;
                s.Reason = $"Erro crítico de autenticação: {errorMessage}";
                return;
            }

            if (EmailErrorClassifier.IsIgnorable(errorMessage))
                return;

            s.Window.Enqueue(false);
            Trim(s.Window);

            if (s.Window.Count >= 3)
            {
                var failures = s.Window.Count(x => !x);
                var rate = (double)failures / s.Window.Count * 100.0;
                if (rate >= MaxErrorRatePercent)
                {
                    s.IsOpen = true;
                    s.Reason = $"Taxa de erro: {rate:F1}%";
                }
            }
        }
    }

    private static void Trim(Queue<bool> q)
    {
        while (q.Count > WindowSize)
            q.Dequeue();
    }
}
