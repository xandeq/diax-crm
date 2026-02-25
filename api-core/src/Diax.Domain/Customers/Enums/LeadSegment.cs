namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Segmento do lead baseado no lead score.
/// Usado pelo sistema de outreach para priorizar envios.
/// </summary>
public enum LeadSegment
{
    /// <summary>
    /// Lead frio - score baixo, menor prioridade.
    /// </summary>
    Cold = 0,

    /// <summary>
    /// Lead morno - score medio, prioridade intermediaria.
    /// </summary>
    Warm = 1,

    /// <summary>
    /// Lead quente - score alto, maior prioridade de contato.
    /// </summary>
    Hot = 2
}
