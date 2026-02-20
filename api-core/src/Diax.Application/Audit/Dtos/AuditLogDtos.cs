using Diax.Domain.Audit;

namespace Diax.Application.Audit.Dtos;

/// <summary>
/// Resposta de um log de auditoria individual.
/// </summary>
public record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string Action,
    string ResourceType,
    string ResourceId,
    string Description,
    string? OldValues,
    string? NewValues,
    string? ChangedProperties,
    string Source,
    string? CorrelationId,
    string? IpAddress,
    DateTime TimestampUtc,
    string Status,
    string? ErrorMessage
);

/// <summary>
/// Resposta paginada da listagem de logs.
/// </summary>
public record AuditLogPagedResponse(
    List<AuditLogResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Parâmetros de filtro recebidos pela API.
/// </summary>
public record AuditLogFilterRequest(
    Guid? UserId = null,
    string? ResourceType = null,
    string? ResourceId = null,
    AuditAction? Action = null,
    AuditSource? Source = null,
    AuditStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchText = null,
    int Page = 1,
    int PageSize = 50
);
