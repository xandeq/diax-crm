namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Origem/método de precificação do valor de um ativo.
/// </summary>
public enum AssetValuationSource
{
    /// <summary>
    /// Preço de mercado (bolsa, cotação spot, etc)
    /// </summary>
    Market = 0,

    /// <summary>
    /// Indexado (CDI, IPCA, índice de referência)
    /// </summary>
    Indexed = 1,

    /// <summary>
    /// Depreciação manual (ex: tabela FIPE aplicada manualmente)
    /// </summary>
    ManualDepreciation = 2,

    /// <summary>
    /// Valor informado manualmente pelo usuário
    /// </summary>
    Manual = 3
}
