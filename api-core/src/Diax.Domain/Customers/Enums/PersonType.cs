namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Tipo de pessoa (física ou jurídica).
/// </summary>
public enum PersonType
{
    /// <summary>
    /// Pessoa Física (CPF).
    /// </summary>
    Individual = 0,

    /// <summary>
    /// Pessoa Jurídica (CNPJ).
    /// </summary>
    Company = 1
}
