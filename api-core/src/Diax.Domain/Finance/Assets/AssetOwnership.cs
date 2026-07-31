namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Titularidade do ativo dentro do casal.
/// </summary>
public enum AssetOwnership
{
    /// <summary>
    /// Ativo do Alexandre
    /// </summary>
    Alexandre = 0,

    /// <summary>
    /// Ativo da esposa
    /// </summary>
    Esposa = 1,

    /// <summary>
    /// Ativo conjunto (do casal)
    /// </summary>
    Conjunto = 2
}
