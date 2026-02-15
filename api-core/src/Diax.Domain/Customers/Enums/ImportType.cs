namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Tipo de importação de leads/customers.
/// </summary>
public enum ImportType
{
    /// <summary>
    /// Importação via arquivo CSV.
    /// </summary>
    CSV = 1,

    /// <summary>
    /// Importação via texto colado (texto livre).
    /// </summary>
    Text = 2
}
