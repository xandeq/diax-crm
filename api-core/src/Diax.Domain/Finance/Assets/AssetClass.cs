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
    /// Dinheiro em espécie / caixa
    /// </summary>
    Dinheiro = 14,

    /// <summary>
    /// FGTS (fonte de capital / saldo)
    /// </summary>
    Fgts = 15,

    /// <summary>
    /// Títulos públicos (Tesouro Direto, etc)
    /// </summary>
    Titulo = 16,

    /// <summary>
    /// ETFs (fundos de índice)
    /// </summary>
    Etf = 17,

    /// <summary>
    /// BDRs (recibos de ativos estrangeiros)
    /// </summary>
    Bdr = 18,

    /// <summary>
    /// Crowdfunding imobiliário
    /// </summary>
    CrowdfundingImob = 19,

    /// <summary>
    /// Carta de crédito (contemplada ou em aquisição)
    /// </summary>
    CartaCredito = 20,

    /// <summary>
    /// Joias
    /// </summary>
    Joias = 21,

    /// <summary>
    /// Obras de arte e colecionáveis
    /// </summary>
    Arte = 22,

    /// <summary>
    /// Propriedade intelectual (marcas, patentes, direitos autorais)
    /// </summary>
    PropriedadeIntelectual = 23,

    /// <summary>
    /// Negócio / operação (exportação, importação, dropshipping, empresa)
    /// </summary>
    Negocio = 24,

    /// <summary>
    /// Outros ativos
    /// </summary>
    Outro = 99
}
