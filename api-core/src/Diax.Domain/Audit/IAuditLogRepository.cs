namespace Diax.Domain.Audit;

/// <summary>
/// Repositório para leitura e persistência de logs de auditoria.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>Persiste um novo log de auditoria (sem SaveChanges).</summary>
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Obtém um log por ID.</summary>
    Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retorna logs paginados com filtros.</summary>
    Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetFilteredAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna o histórico completo de um recurso específico.</summary>
    Task<List<AuditLogEntry>> GetByResourceAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna as últimas ações executadas por um usuário.</summary>
    Task<List<AuditLogEntry>> GetByUserAsync(
        Guid userId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>Remove logs mais antigos que a data informada. Retorna quantidade deletada.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
