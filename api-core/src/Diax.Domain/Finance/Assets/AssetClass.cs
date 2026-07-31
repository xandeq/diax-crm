namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Classes de ativos do módulo Patrimônio &amp; Investimentos.
/// </summary>
public enum AssetClass
{
    /// <summary>
    /// Ouro físico ou financeiro
    /// </summary>
    Ouro = 0,

    /// <summary>
    /// Diamantes e pedras preciosas
    /// </summary>
    Diamante = 1,

    /// <summary>
    /// Ações (renda variável)
    /// </summary>
    Acao = 2,

    /// <summary>
    /// Fundos imobiliários
    /// </summary>
    Fii = 3,

    /// <summary>
    /// Renda fixa (CDB, Tesouro, LCI/LCA, etc)
    /// </summary>
    RendaFixa = 4,

    /// <summary>
    /// Fundos de investimento
    /// </summary>
    Fundo = 5,

    /// <summary>
    /// Fundos multimercado
    /// </summary>
    Multimercado = 6,

    /// <summary>
    /// Veículos (carro, moto, etc)
    /// </summary>
    Veiculo = 7,

    /// <summary>
    /// Imóveis
    /// </summary>
    Imovel = 8,

    /// <summary>
    /// Consórcios
    /// </summary>
    Consorcio = 9,

    /// <summary>
    /// Moeda estrangeira em espécie ou conta (USD, EUR, etc)
    /// </summary>
    Moeda = 10,

    /// <summary>
    /// Investimentos no exterior
    /// </summary>
    Exterior = 11,

    /// <summary>
    /// Milhas e pontos de fidelidade
    /// </summary>
    Milhas = 12,

    /// <summary>
    /// Criptomoedas
    /// </summary>
    Cripto = 13,

    /// <summary>
    /// Outros ativos
    /// </summary>
    Outro = 99
}
