namespace Diax.Domain.ErrorLogs;

public interface IErrorLogRepository
{
    Task AddAsync(ErrorLog log, CancellationToken ct = default);
    /// <summary>Leitura sem tracking — para exibição somente.</summary>
    Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Leitura com tracking — necessário antes de mutações (Resolve).</summary>
    Task<ErrorLog?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<ErrorLog?> GetOpenByFingerprintAsync(string fingerprint, string appName, CancellationToken ct = default);
    Task<(IReadOnlyList<ErrorLog> Items, int TotalCount)> GetFilteredAsync(ErrorLogFilter filter, CancellationToken ct = default);
    Task<ErrorLogStats> GetStatsAsync(CancellationToken ct = default);
    /// <summary>Incrementa OccurrenceCount + LastSeenAt em uma única operação SQL atômica (sem read-modify-write).</summary>
    Task<bool> IncrementOccurrenceAtomicAsync(string fingerprint, string appName, DateTime occurredAt, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public record ErrorLogStats(
    int TotalToday,
    int CriticalToday,
    int UnresolvedTotal,
    IReadOnlyList<AppErrorCount> ByApp);

public record AppErrorCount(string AppName, int Count);
