namespace Diax.Application.EmailMarketing.Dispatch;

public interface IProviderQuotaGuard
{
    /// <summary>
    /// Tenta consumir 1 envio do limite diário do provider.
    /// Retorna false se o limite foi atingido (não consome).
    /// Thread-safe. Reseta à meia-noite UTC.
    /// </summary>
    bool TryConsume(string providerKey);

    /// <summary>
    /// Retorna o número de envios restantes no dia para o provider.
    /// Retorna int.MaxValue quando não há limite configurado.
    /// </summary>
    int GetRemaining(string providerKey);

    /// <summary>
    /// Snapshot do estado atual de quota de todos os providers conhecidos.
    /// </summary>
    IReadOnlyDictionary<string, ProviderQuotaStatus> GetStatus();
}

public sealed record ProviderQuotaStatus(string Provider, int Used, int DailyLimit, int Remaining, DateTime ResetAtUtc);
