namespace Diax.Domain.Audit;

/// <summary>
/// Resultado da execução da ação auditada.
/// </summary>
public enum AuditStatus
{
    /// <summary>Ação concluída com sucesso.</summary>
    Success = 0,

    /// <summary>Ação concluída parcialmente (ex: bulk com falhas individuais).</summary>
    PartialSuccess = 1,

    /// <summary>Ação falhou.</summary>
    Failed = 2
}
