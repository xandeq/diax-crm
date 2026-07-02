namespace Diax.Application.EmailMarketing.Dispatch;

public interface IProviderCircuitBreaker
{
    /// <summary>
    /// True enquanto o breaker está aberto E o cooldown não venceu. Após o cooldown o
    /// breaker entra em half-open: libera UMA tentativa de prova (as demais continuam
    /// bloqueadas) — sucesso fecha o circuito, falha reabre e reinicia o cooldown.
    /// </summary>
    bool IsOpen(string providerKey);

    void RecordSuccess(string providerKey);
    void RecordFailure(string providerKey, string? errorMessage);

    /// <summary>Fecha manualmente o breaker de um provider e limpa a janela de falhas.</summary>
    void Reset(string providerKey);

    /// <summary>Snapshot do estado de todos os providers conhecidos (para status/admin).</summary>
    IReadOnlyDictionary<string, ProviderBreakerStatus> GetStates();
}

public sealed record ProviderBreakerStatus(
    string Provider,
    bool IsOpen,
    bool IsHalfOpen,
    string? Reason,
    DateTime? OpenedAtUtc,
    double ErrorRatePercent);
