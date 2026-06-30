namespace Diax.Application.EmailMarketing.Dispatch;

public interface IProviderQuotaGuard
{
    /// <summary>
    /// Tenta consumir 1 envio — verifica limite DIÁRIO e SEMANAL.
    /// Retorna false se qualquer um dos limites foi atingido.
    /// Thread-safe. Diário reseta à meia-noite UTC; semanal na segunda-feira 00:00 UTC.
    /// </summary>
    bool TryConsume(string providerKey);

    /// <summary>
    /// Retorna o número de envios restantes (mínimo entre diário e semanal).
    /// Retorna int.MaxValue quando não há limite configurado em nenhuma escala.
    /// </summary>
    int GetRemaining(string providerKey);

    /// <summary>
    /// Snapshot do estado atual de quota (diária + semanal) de todos os providers conhecidos.
    /// </summary>
    IReadOnlyDictionary<string, ProviderQuotaStatus> GetStatus();
}

public sealed record ProviderQuotaStatus(
    string Provider,
    int DailyUsed,
    int DailyLimit,
    int DailyRemaining,
    DateTime DailyResetAtUtc,
    int WeeklyUsed,
    int WeeklyLimit,
    int WeeklyRemaining,
    DateTime WeeklyResetAtUtc);
