namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Indica o tipo de e-mail identificado, útil para segmentação de marketing.
/// </summary>
public enum EmailType
{
    /// <summary>
    /// E-mail indefinido ou não classificado.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// E-mail pessoal ou contato direto (ex: joao@empresa.com, maria@gmail.com).
    /// </summary>
    PersonalDirect = 1,

    /// <summary>
    /// E-mail genérico ou de departamento (ex: contato@, rh@, financeiro@).
    /// </summary>
    GenericCorporate = 2
}
