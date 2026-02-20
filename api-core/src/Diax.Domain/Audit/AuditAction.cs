namespace Diax.Domain.Audit;

/// <summary>
/// Tipos de ações auditáveis no sistema.
/// </summary>
public enum AuditAction
{
    /// <summary>Novo recurso criado.</summary>
    Create = 0,

    /// <summary>Recurso atualizado.</summary>
    Update = 1,

    /// <summary>Recurso deletado (soft ou hard).</summary>
    Delete = 2,

    /// <summary>Status do recurso alterado explicitamente.</summary>
    StatusChange = 3,

    /// <summary>Lead convertido para Cliente.</summary>
    ConvertLead = 4,

    /// <summary>Campanha de email enfileirada.</summary>
    CampaignQueued = 5,

    /// <summary>Transação financeira criada/removida.</summary>
    FinancialTransaction = 6,

    /// <summary>Login de usuário.</summary>
    Login = 7,

    /// <summary>Logout de usuário.</summary>
    Logout = 8,

    /// <summary>Alteração de permissões ou grupos.</summary>
    PermissionChange = 9,

    /// <summary>Importação em lote.</summary>
    BulkImport = 10,

    /// <summary>Ação customizada/manual.</summary>
    Custom = 99
}
