using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Diax.Application.EmailMarketing;

/// <summary>
/// Implementação em memória e thread-safe do Circuit Breaker para a campanha piloto.
/// </summary>
public class PilotCircuitBreaker : IPilotCircuitBreaker
{
    private readonly object _lock = new();
    private bool _isOpen;
    private string? _reason;
    private int _webhookFailureCount;
    
    // Rastreia os últimos 10 disparos (true = sucesso, false = falha)
    private readonly ConcurrentQueue<bool> _recentSends = new();
    private const int SlidingWindowSize = 10;
    private const double MaxErrorRatePercent = 30.0; // limite de 30% de erro

    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                return _isOpen;
            }
        }
    }

    public string? Reason
    {
        get
        {
            lock (_lock)
            {
                return _reason;
            }
        }
    }

    public int WebhookFailureCount
    {
        get
        {
            lock (_lock)
            {
                return _webhookFailureCount;
            }
        }
    }

    public double CurrentErrorRate
    {
        get
        {
            var sends = _recentSends.ToArray();
            if (sends.Length == 0) return 0.0;

            var failures = sends.Count(s => !s);
            return (double)failures / sends.Length * 100.0;
        }
    }

    public void Open(string reason)
    {
        lock (_lock)
        {
            _isOpen = true;
            _reason = reason;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _isOpen = false;
            _reason = null;
            _webhookFailureCount = 0;
            while (_recentSends.TryDequeue(out _)) { }
        }
    }

    public void RecordSuccess()
    {
        _recentSends.Enqueue(true);
        TrimSlidingWindow();
        ResetWebhookFailure();
    }

    public void RecordFailure(string errorMsg)
    {
        // Erros de rate-limit (HTTP 429) são transitórios: o provedor está
        // momentaneamente saturado, não há problema de credencial nem de dado.
        // Não contam para a taxa de erro nem abrem o circuito — o item volta para
        // retry (e a rotação tende a usar outro provedor na próxima tentativa).
        if (IsTransientRateLimit(errorMsg))
        {
            return;
        }

        _recentSends.Enqueue(false);
        TrimSlidingWindow();

        lock (_lock)
        {
            // Se for erro crítico (Ex: bounce crítico ou autenticação) ou se passar da taxa de erro
            if (errorMsg.Contains("bounce", StringComparison.OrdinalIgnoreCase) || 
                errorMsg.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || 
                errorMsg.Contains("autenticação", StringComparison.OrdinalIgnoreCase) || 
                errorMsg.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                errorMsg.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                errorMsg.Contains("invalid api key", StringComparison.OrdinalIgnoreCase) ||
                errorMsg.Contains("apikey", StringComparison.OrdinalIgnoreCase))
            {
                Open($"Erro crítico de envio detectado: {errorMsg}");
                return;
            }

            if (CurrentErrorRate >= MaxErrorRatePercent && _recentSends.Count >= 3)
            {
                Open($"Taxa de erro excedeu o limite tolerado: {CurrentErrorRate:F1}%");
            }
        }
    }

    public void RecordWebhookFailure()
    {
        lock (_lock)
        {
            _webhookFailureCount++;
            if (_webhookFailureCount >= 3)
            {
                Open($"Falhas consecutivas de webhook detectadas: {_webhookFailureCount} falhas.");
            }
        }
    }

    public void ResetWebhookFailure()
    {
        lock (_lock)
        {
            _webhookFailureCount = 0;
        }
    }

    private void TrimSlidingWindow()
    {
        while (_recentSends.Count > SlidingWindowSize)
        {
            _recentSends.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Detecta erros de limite de taxa (HTTP 429 / "Too Many Requests" / "rate limit"),
    /// que são transitórios e não devem disparar o circuit breaker.
    /// </summary>
    private static bool IsTransientRateLimit(string errorMsg)
    {
        if (string.IsNullOrEmpty(errorMsg))
        {
            return false;
        }

        return errorMsg.Contains("429", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("too many requests", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("rate_limit", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("ratelimit", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("too_many_requests", StringComparison.OrdinalIgnoreCase);
    }
}
