namespace Diax.Domain.Audit;

/// <summary>
/// Contexto/origem da execução da ação auditada.
/// </summary>
public enum AuditSource
{
    /// <summary>Via API REST autenticada.</summary>
    Api = 0,

    /// <summary>Trigger automático (scheduler, worker, evento).</summary>
    Automation = 1,

    /// <summary>Ação interna do sistema (seed, manutenção).</summary>
    System = 2,

    /// <summary>Importação em lote de dados externos.</summary>
    BulkImport = 3,

    /// <summary>Migration de dados (script ou EF migration).</summary>
    Migration = 4
}
