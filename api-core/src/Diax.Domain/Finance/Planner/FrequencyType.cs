namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Tipo de frequência para transações recorrentes
/// </summary>
public enum FrequencyType
{
    /// <summary>
    /// Diária
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Semanal
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Mensal
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Trimestral
    /// </summary>
    Quarterly = 4,

    /// <summary>
    /// Anual
    /// </summary>
    Yearly = 5
}
