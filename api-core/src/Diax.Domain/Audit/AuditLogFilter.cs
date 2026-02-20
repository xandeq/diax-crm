namespace Diax.Domain.Audit;

/// <summary>
/// Parâmetros de filtro para consulta de logs de auditoria.
/// </summary>
public class AuditLogFilter
{
    public Guid? UserId { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public AuditAction? Action { get; set; }
    public AuditSource? Source { get; set; }
    public AuditStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>Busca livre por ResourceId, ResourceType ou Description.</summary>
    public string? SearchText { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
