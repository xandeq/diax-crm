using Diax.Domain.Common;

namespace Diax.Domain.Logs;

/// <summary>
/// Repositório para operações com logs da aplicação.
/// </summary>
public interface IAppLogRepository : IRepository<AppLog>
{
    /// <summary>
    /// Busca logs com filtros e paginação.
    /// </summary>
    Task<(IReadOnlyList<AppLog> Items, int TotalCount)> GetFilteredAsync(
        DateTime? fromDate,
        DateTime? toDate,
        LogLevel? level,
        LogCategory? category,
        string? search,
        string? userId,
        string? correlationId,
        string? requestId,
        string? path,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém estatísticas de logs por nível.
    /// </summary>
    Task<Dictionary<LogLevel, int>> GetStatsByLevelAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove logs anteriores à data especificada.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
