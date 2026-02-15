namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Status do processo de importação.
/// </summary>
public enum ImportStatus
{
    /// <summary>
    /// Importação em processamento.
    /// </summary>
    Processing = 0,

    /// <summary>
    /// Importação concluída com sucesso (100% dos registros).
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Importação falhou completamente (0% dos registros).
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Importação parcialmente bem-sucedida (alguns registros falharam).
    /// </summary>
    PartialSuccess = 3
}
