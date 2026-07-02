namespace Diax.Application.EmailMarketing.Dispatch;

public interface IProviderQuotaGuard
{
    /// <summary>
    /// Tenta consumir 1 envio — verifica limite DIÁRIO e SEMANAL.
    /// Retorna false se qualquer um dos limites foi atingido.
    /// Thread-safe. Diário reseta à meia-noite UTC; semanal na segunda-feira 00:00 UTC.
    /// No primeiro consumo de cada período o contador é hidratado do banco (envios já
    /// registrados), para que um recycle do processo não zere a quota no meio do dia.
    /// </summary>
    Task<bool> TryConsumeAsync(string providerKey, CancellationToken ct = default);

    /// <summary>
    /// Retorna o número de envios restantes (mínimo entre diário e semanal).
    /// Retorna int.MaxValue quando não há limite configurado em nenhuma escala.
    /// </summary>
    Task<int> GetRemainingAsync(string providerKey, CancellationToken ct = default);

    /// <summary>
    /// Snapshot do estado atual de quota (diária + semanal) de todos os providers conhecidos.
    /// </summary>
    Task<IReadOnlyDictionary<string, ProviderQuotaStatus>> GetStatusAsync(CancellationToken ct = default);
}

/// <summary>
/// Fonte durável de uso por provider — consulta o banco para hidratar os contadores
/// in-memory após restart/recycle do processo.
/// </summary>
public interface IProviderQuotaUsageSource
{
    Task<int> GetUsedSinceAsync(string providerKey, DateTime sinceUtc, CancellationToken ct = default);
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
