namespace Diax.Domain.Logs;

/// <summary>
/// Níveis de log disponíveis no sistema.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Informações de debug (desenvolvimento)
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Informações gerais sobre o funcionamento do sistema
    /// </summary>
    Information = 1,

    /// <summary>
    /// Avisos que não impedem o funcionamento mas requerem atenção
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Erros que afetam funcionalidades específicas
    /// </summary>
    Error = 3,

    /// <summary>
    /// Erros críticos que podem afetar o sistema todo
    /// </summary>
    Critical = 4
}
