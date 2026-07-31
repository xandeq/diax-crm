namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Grau de liquidez de um ativo do patrimônio.
/// </summary>
public enum AssetLiquidity
{
    /// <summary>
    /// Líquido — resgatável imediatamente ou em poucos dias
    /// </summary>
    Liquido = 0,

    /// <summary>
    /// Ilíquido — venda demorada (imóveis, veículos, etc)
    /// </summary>
    Iliquido = 1,

    /// <summary>
    /// Travado — bloqueado até data/condição (ex: RF com carência)
    /// </summary>
    Travado = 2,

    /// <summary>
    /// Contingente — valor incerto/condicional; NÃO entra no total realizável
    /// </summary>
    Contingente = 3
}
