using Diax.Domain.Common;

namespace Diax.Domain.Audit;

/// <summary>
/// Registra cada ação executada no sistema para fins de auditoria e compliance.
/// Herda de Entity (não de AuditableEntity) para evitar loop no interceptor.
/// </summary>
public class AuditLogEntry : Entity
{
    /// <summary>ID do usuário que executou a ação (nulo para ações de sistema).</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Tipo de ação executada.</summary>
    public AuditAction Action { get; private set; }

    /// <summary>Tipo do recurso afetado (ex: "Customer", "Expense", "Lead").</summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>ID do recurso afetado.</summary>
    public string ResourceId { get; private set; } = string.Empty;

    /// <summary>Descrição legível da ação.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Estado anterior do recurso em JSON (null em Create).</summary>
    public string? OldValues { get; private set; }

    /// <summary>Estado novo do recurso em JSON (null em Delete).</summary>
    public string? NewValues { get; private set; }

    /// <summary>Nomes das propriedades alteradas, separados por vírgula (somente em Update).</summary>
    public string? ChangedProperties { get; private set; }

    /// <summary>Origem/contexto da ação.</summary>
    public AuditSource Source { get; private set; } = AuditSource.Api;

    /// <summary>ID de correlação para rastrear requests distribuídos.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>IP de origem da ação.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Timestamp UTC de quando a ação foi executada.</summary>
    public DateTime TimestampUtc { get; private set; }

    /// <summary>Status da execução da ação.</summary>
    public AuditStatus Status { get; private set; } = AuditStatus.Success;

    /// <summary>Mensagem de erro se o status for Failed ou PartialSuccess.</summary>
    public string? ErrorMessage { get; private set; }

    // ===== EF Core =====
    protected AuditLogEntry() { }

    private AuditLogEntry(
        Guid? userId,
        AuditAction action,
        string resourceType,
        string resourceId,
        string description,
        AuditSource source,
        string? correlationId,
        string? ipAddress)
    {
        UserId = userId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Description = description;
        Source = source;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        TimestampUtc = DateTime.UtcNow;
        Status = AuditStatus.Success;
    }

    // ===== Factory =====

    public static AuditLogEntry Create(
        Guid? userId,
        AuditAction action,
        string resourceType,
        string resourceId,
        string description,
        string? oldValues = null,
        string? newValues = null,
        string? changedProperties = null,
        AuditSource source = AuditSource.Api,
        string? correlationId = null,
        string? ipAddress = null)
    {
        var entry = new AuditLogEntry(
            userId, action, resourceType, resourceId,
            description, source, correlationId, ipAddress)
        {
            OldValues = oldValues,
            NewValues = newValues,
            ChangedProperties = changedProperties
        };

        return entry;
    }

    // ===== Mutations =====

    public void MarkAsFailed(string errorMessage)
    {
        Status = AuditStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void MarkAsPartialSuccess(string errorMessage)
    {
        Status = AuditStatus.PartialSuccess;
        ErrorMessage = errorMessage;
    }
}
