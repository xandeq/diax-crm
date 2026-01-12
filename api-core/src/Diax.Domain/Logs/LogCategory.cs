namespace Diax.Domain.Logs;

/// <summary>
/// Categorias de log para classificação e filtragem.
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// Logs do sistema (startup, shutdown, etc.)
    /// </summary>
    System = 0,

    /// <summary>
    /// Logs relacionados à segurança (autenticação, autorização)
    /// </summary>
    Security = 1,

    /// <summary>
    /// Logs de auditoria (ações de usuários)
    /// </summary>
    Audit = 2,

    /// <summary>
    /// Logs de regras de negócio
    /// </summary>
    Business = 3,

    /// <summary>
    /// Logs de performance
    /// </summary>
    Performance = 4,

    /// <summary>
    /// Logs de integração com sistemas externos
    /// </summary>
    Integration = 5
}
