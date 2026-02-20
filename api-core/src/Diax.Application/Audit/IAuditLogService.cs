using Diax.Application.Audit.Dtos;
using Diax.Domain.Audit;
using Diax.Shared.Results;

namespace Diax.Application.Audit;

/// <summary>
/// Serviço de consulta e administração de logs de auditoria.
/// A criação automática é feita pelo AuditSaveChangesInterceptor.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Cria um log de auditoria manual (para ações explícitas como Login, ConvertLead, etc.).
    /// Não quebra a operação principal em caso de falha.
    /// </summary>
    Task<Result<Guid>> LogManualAsync(
        Guid? userId,
        AuditAction action,
        string resourceType,
        string resourceId,
        string description,
        string? oldValues = null,
        string? newValues = null,
        AuditSource source = AuditSource.Api,
        string? correlationId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém um log de auditoria por ID.</summary>
    Task<Result<AuditLogResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retorna logs paginados com filtros combinados.</summary>
    Task<Result<AuditLogPagedResponse>> GetFilteredAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna o histórico completo de um recurso específico.</summary>
    Task<Result<List<AuditLogResponse>>> GetResourceHistoryAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna as ações mais recentes de um usuário.</summary>
    Task<Result<List<AuditLogResponse>>> GetUserActivityAsync(
        Guid userId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove logs mais antigos que olderThanDays dias.
    /// Retenta apenas se olderThanDays >= 30.
    /// </summary>
    Task<Result<int>> CleanupOldLogsAsync(
        int olderThanDays,
        CancellationToken cancellationToken = default);
}
