using Diax.Application.Common;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Oportunidades diárias de investimento (F2).
/// Refresh idempotente por dia-calendário: InvestIQ (best-effort, nunca bloqueia)
/// + lista curada de ideias (sempre semeada). Ordenadas por EaseRank e depois Score.
/// </summary>
public class OpportunityService : IApplicationService
{
    private readonly IInvestmentOpportunityRepository _repository;
    private readonly IInvestIQIntegrationService _investIQ;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OpportunityService> _logger;

    public OpportunityService(
        IInvestmentOpportunityRepository repository,
        IInvestIQIntegrationService investIQ,
        IUnitOfWork unitOfWork,
        ILogger<OpportunityService> logger)
    {
        _repository = repository;
        _investIQ = investIQ;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gera as oportunidades do dia (idempotente por dia-calendário UTC).
    /// Se já houver oportunidades geradas hoje, retorna as existentes sem regenerar.
    /// </summary>
    public async Task<Result<IEnumerable<OpportunityResponse>>> RefreshDailyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            _logger.LogInformation("Refreshing daily opportunities for user {UserId} ({Day})", userId, today);

            var existing = await _repository.GetByGeneratedDateAsync(userId, today, cancellationToken);
            if (existing.Count > 0)
            {
                _logger.LogInformation(
                    "Opportunities already generated today for user {UserId} ({Count}), skipping refresh",
                    userId, existing.Count);
                return Result<IEnumerable<OpportunityResponse>>.Success(existing.Select(MapToResponse));
            }

            var generatedAt = DateTime.UtcNow;
            var opportunities = new List<InvestmentOpportunity>();

            // (a) InvestIQ — best-effort: falha/não-configurado NUNCA bloqueia a geração
            var investIQResult = await _investIQ.GetDailyOpportunitiesAsync(cancellationToken);
            if (investIQResult.IsSuccess)
            {
                foreach (var item in investIQResult.Value)
                {
                    if (string.IsNullOrWhiteSpace(item.Title))
                        continue;

                    opportunities.Add(InvestmentOpportunity.Create(
                        userId,
                        MapInvestIQClass(item.AssetClass),
                        item.Title,
                        item.Thesis ?? string.Empty,
                        InvestmentOpportunity.SourceInvestIQ,
                        item.Risk ?? "medio",
                        generatedAt,
                        score: item.Score,
                        ticker: item.Ticker,
                        easeRank: null,
                        suggestedAllocationPct: item.SuggestedAllocationPct));
                }

                _logger.LogInformation(
                    "InvestIQ returned {Count} opportunities for user {UserId}", investIQResult.Value.Count, userId);
            }
            else
            {
                _logger.LogWarning(
                    "InvestIQ opportunities unavailable for user {UserId}: {ErrorCode} - {ErrorMessage}. Continuing with curated ideas only.",
                    userId, investIQResult.Error.Code, investIQResult.Error.Message);
            }

            // (b) Lista curada de ideias — SEMPRE semeada
            opportunities.AddRange(BuildCuratedIdeas(userId, generatedAt));

            await _repository.AddRangeAsync(opportunities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated {Count} opportunities for user {UserId}", opportunities.Count, userId);

            var ordered = opportunities
                .OrderBy(o => o.EaseRank ?? int.MaxValue)
                .ThenByDescending(o => o.Score)
                .Select(MapToResponse);

            return Result<IEnumerable<OpportunityResponse>>.Success(ordered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh daily opportunities for user {UserId}", userId);
            return Result.Failure<IEnumerable<OpportunityResponse>>(
                new Error("Opportunity.RefreshFailed", "Failed to refresh opportunities. Please check server logs for details."));
        }
    }

    /// <summary>
    /// Retorna as oportunidades de hoje. Se ainda não foram geradas, dispara o refresh.
    /// </summary>
    public async Task<Result<IEnumerable<OpportunityResponse>>> GetDailyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var opportunities = await _repository.GetByGeneratedDateAsync(userId, today, cancellationToken);
            if (opportunities.Count == 0)
            {
                return await RefreshDailyAsync(userId, cancellationToken);
            }

            return Result<IEnumerable<OpportunityResponse>>.Success(opportunities.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve daily opportunities for user {UserId}", userId);
            return Result.Failure<IEnumerable<OpportunityResponse>>(
                new Error("Opportunity.QueryFailed", "Failed to retrieve opportunities. Please check server logs for details."));
        }
    }

    /// <summary>
    /// Lista curada de ideias (Source="idea"), ordenada por facilidade de execução (EaseRank 1 = mais fácil).
    /// </summary>
    private static List<InvestmentOpportunity> BuildCuratedIdeas(Guid userId, DateTime generatedAt)
    {
        var ideas = new (AssetClass Class, string? Ticker, string Title, string Thesis, int EaseRank, decimal Score, string Risk)[]
        {
            (AssetClass.RendaFixa, null, "Tesouro Selic",
                "Reserva de emergência e caixa remunerado a ~100% da Selic com liquidez diária e risco soberano. Primeiro degrau de qualquer carteira.",
                1, 90m, "baixo"),
            (AssetClass.RendaFixa, null, "LCI/LCA/CDB 100%+ do CDI",
                "Renda fixa bancária isenta de IR (LCI/LCA) ou com taxa acima do CDI (CDB), protegida pelo FGC até R$250 mil por instituição.",
                2, 88m, "baixo"),
            (AssetClass.RendaFixa, null, "Tesouro IPCA+ / Debênture incentivada",
                "Proteção contra inflação com juro real garantido no vencimento; debêntures incentivadas são isentas de IR para pessoa física.",
                3, 86m, "baixo"),
            (AssetClass.Acao, null, "Ações pagadoras de dividendos",
                "Empresas maduras (bancos, energia, saneamento) com dividend yield consistente geram renda passiva e tendem a cair menos em crises.",
                4, 84m, "medio"),
            (AssetClass.Etf, "IVVB11", "ETF S&P 500 (IVVB11)",
                "Exposição às 500 maiores empresas dos EUA em reais, com dolarização implícita da carteira em um único ativo na B3.",
                5, 85m, "medio"),
            (AssetClass.Exterior, "VT", "ETF de mundo (VT)",
                "Diversificação global máxima em um único ETF (milhares de empresas de dezenas de países) via conta internacional.",
                6, 82m, "medio"),
            (AssetClass.Ouro, "GOLD11", "Ouro (GOLD11)",
                "Hedge clássico contra crises e desvalorização de moedas; baixa correlação com bolsa e renda fixa local.",
                7, 78m, "medio"),
            (AssetClass.Exterior, null, "Treasuries / ETF de renda fixa USD",
                "Renda fixa do governo americano: rendimento em dólar com o menor risco de crédito do mundo; dolariza a carteira com volatilidade baixa.",
                8, 80m, "baixo"),
            (AssetClass.Cripto, null, "Stablecoin yield",
                "Rendimento em dólar (8-12% a.a.) sobre stablecoins em plataformas consolidadas; atenção ao risco de contraparte e custódia.",
                9, 70m, "alto"),
            (AssetClass.Fii, null, "FII de papel (CRI)",
                "Fundos de recebíveis imobiliários pagam dividendos mensais isentos de IR, indexados a CDI ou IPCA — renda sem imóvel físico.",
                10, 79m, "medio"),
            (AssetClass.Fundo, null, "FIDC",
                "Fundos de direitos creditórios oferecem taxas acima do CDI ao financiar recebíveis de empresas; exigem análise da carteira de crédito.",
                11, 68m, "alto"),
            (AssetClass.Imovel, null, "Imóvel na planta",
                "Compra na planta com desconto vs pronto (15-30%) e valorização até a entrega; risco de atraso da obra e distrato.",
                12, 65m, "medio"),
            (AssetClass.Imovel, null, "Leilão Caixa",
                "Imóveis retomados pela Caixa com deságio de 30-50% do valor de avaliação; exige análise jurídica da matrícula e da ocupação.",
                13, 72m, "alto"),
            (AssetClass.Negocio, null, "Airbnb da herança",
                "Colocar imóvel herdado/ocioso em short-stay (Airbnb) pode render 2-3x o aluguel tradicional, transformando patrimônio parado em fluxo de caixa.",
                14, 75m, "medio"),

            // ─── BENS PALPÁVEIS (foco em imóvel, ouro, joias, carro, arte) ───
            (AssetClass.Veiculo, null, "Seminovo 1-2 anos à vista (PJ)",
                "A depreciação forte (20-30%) do 1º ano já foi absorvida pelo dono anterior. Dica: compre à vista pelo CNPJ (negocia 5-15% e lança como ativo). Carro comum é consumo — só troque se necessário.",
                15, 74m, "baixo"),
            (AssetClass.Ouro, null, "Ouro físico (barra/moeda)",
                "Hedge portátil, fora do sistema bancário. Dica: compre POR PESO em DTVM/distribuidora idônea (Ourominas, OM), nunca em joalheria; equilibre metade físico (proteção) + metade GOLD11 (liquidez).",
                16, 77m, "medio"),
            (AssetClass.Imovel, null, "Leilão judicial/extrajudicial",
                "Deságio de 30-50% vs mercado. Dica: leia a matrícula, cheque dívidas de IPTU/condomínio e o status de ocupação ANTES do lance, e reserve caixa pra eventual desocupação.",
                17, 71m, "alto"),
            (AssetClass.Imovel, null, "Loteamento em expansão",
                "Terreno em vetor de crescimento: ticket baixo, quase zero manutenção, valoriza com a infraestrutura chegando. Dica: parcele direto com a loteadora e priorize lotes de esquina/frente pra revenda.",
                18, 69m, "medio"),
            (AssetClass.Imovel, null, "Imóvel com inquilino (renda dia 1)",
                "Comprar já alugado trava fluxo de caixa desde a assinatura. Dica: mire cap rate (aluguel anual ÷ preço) ≥ 6% a.a. e histórico de pagamento limpo do inquilino.",
                19, 67m, "medio"),
            (AssetClass.Imovel, null, "Sala comercial / laje corporativa",
                "Yield costuma superar o residencial (0,7-1% a.m.). Dica: localização e vacância mandam — prefira lajes com inquilino corporativo e contrato longo (built-to-suit/típico).",
                20, 63m, "medio"),
            (AssetClass.Imovel, null, "Terra rural / agrícola",
                "Ativo real que valoriza no longo prazo e ainda gera renda de arrendamento. Dica: cheque CAR, matrícula e aptidão do solo; arrende pra produtor estabelecido da região.",
                21, 64m, "medio"),
            (AssetClass.Joias, null, "Ouro 18k+ por peso / pedras certificadas",
                "Joia de varejo (shopping) NÃO é investimento — o markup passa de 100%. Dica: compre ouro POR GRAMA e pedras com certificado (GIA/IGI); a revenda é por peso/leilão, nunca de volta ao varejo.",
                22, 58m, "alto"),
            (AssetClass.Joias, null, "Relógios de luxo (Rolex/AP)",
                "Modelos icônicos têm mercado secundário líquido e alguns valorizam acima da inflação. Dica: compre referências consagradas com caixa + documentos completos; fuja de peças sem procedência.",
                23, 60m, "medio"),
            (AssetClass.Veiculo, null, "Carro clássico / colecionável",
                "Ao contrário do carro comum, clássicos raros e bem conservados valorizam. Dica: só entre se conhece o nicho — procedência, originalidade e histórico de manutenção definem o preço.",
                24, 55m, "alto"),
            (AssetClass.Arte, null, "Obras de arte (galeria/leilão)",
                "Ativo descorrelacionado do mercado financeiro, com potencial de valorização de artistas em ascensão. Dica: compre com curadoria, guarde certificados de autenticidade; é ilíquido e de longo prazo.",
                25, 52m, "alto"),
            (AssetClass.Arte, null, "Vinho / whisky de investimento",
                "Safras raras e casks valorizam em plataformas de investimento (en primeur). Dica: nicho — comece pequeno, priorize rótulos com liquidez de revenda e armazenagem certificada.",
                26, 50m, "alto"),
            (AssetClass.Semovente, null, "Gado de corte (recria/engorda)",
                "Ativo real cotado em arroba, com liquidez via leilão/frigorífico e histórico de acompanhar a inflação. Dica: comece por recria-engorda em parceria (boi de capim) ou pelo lado financeiro (FIAGRO/fundos do agro) antes de comprar cabeça própria.",
                27, 57m, "alto"),
            (AssetClass.Semovente, null, "Cavalo de genética (Quarto de Milha/Mangalarga)",
                "Valorização vem de genética + desempenho comprovado em prova, não do animal comum. Dica: só entre com assessoria de haras e registro (ABQM/ABCCMM); custo mensal de cocheira e veterinário corrói o retorno de quem não é do meio.",
                28, 48m, "alto"),

            // ─── 🎯 RADAR ES (pesquisa 03/08/2026 — terrenos ES + consórcio auto/carro surf) ───
            (AssetClass.Imovel, null, "🎯 ES: Loteamento CBL — Serra Ville / Alta Ville (Aracruz)",
                "Maior loteadora do ES: entrada ~10%, financiamento direto SEM análise de crédito, até 48x fixas. Tese dupla: Serra pega o Contorno do Mestre Álvaro (conclui fim de 2026; m² da Serra +110% em 4 anos) e Aracruz pega porto Imetame (opera 2026) + fábrica GWM. Ação: visitar plantão CBL (lotescbl.com.br) e priorizar lote de esquina/frente.",
                15, 89m, "medio"),
            (AssetClass.Imovel, null, "🎯 ES: Leilão 2ª praça — Serra/Cariacica/Viana (40-67% off)",
                "Agregadores mostram ES com até 67% de deságio (Vila Velha 57%, Serra 52%, Cariacica 51%). Exemplo real: lote na Serra avaliado R$452k saiu a R$242,8k na 2ª praça (-40%). Ação: alertas de 'terreno até R$150k' em leilaoimovel.com.br/es, spyleiloes e leilaoninja (379 imóveis na Serra); checar matrícula, IPTU e ocupação ANTES do lance.",
                16, 87m, "alto"),
            (AssetClass.Imovel, null, "🎯 ES: Terreno urbano em Aracruz pré-porto",
                "Porto Imetame começa a operar em 2026 e a GWM se instala na cidade — demanda por moradia e logística sobe, e o terreno urbano ainda é barato (assimetria alta). Prefeitura planeja leilões de áreas no 2º semestre/2026. Ação: monitorar Barra do Riacho e entorno antes da abertura do porto.",
                16, 86m, "medio"),
            (AssetClass.Imovel, null, "🎯 ES: Litoral sul 2ª quadra — Anchieta/Piúma/Guarapari",
                "Samarco investe R$13 bi na retomada (100% da capacidade até 2028, pelotização em Ubu/Anchieta) e a orla de Guarapari já bate R$9k/m² — a 2ª quadra ainda não precificou. Anúncio real: Anchieta 360m² por R$144,9k. Cuidado: terreno de marinha na 1ª faixa (laudêmio ~5% SPU).",
                17, 85m, "medio"),
            (AssetClass.CartaCredito, null, "🎯 Consórcio auto: carta 70-80k + lance embutido",
                "Rodobens (taxa desde 6,5%) ou Embracon, carta R$70-80k ≈ R$1.4-1.6k/mês em 60m. Entrar em grupo EM ANDAMENTO (contempla mais fácil, histórico de lances visível) e dar lance embutido 25% + 10-15% livre do bolso — média vencedora em grupos independentes é 22-32% → contemplação estimada em 3-8 meses. FGTS NÃO vale p/ auto. Fugir de taxa adm acima de 20%.",
                4, 87m, "baixo"),
            (AssetClass.Veiculo, null, "🎯 Carro de surf econômico: Fit / Cronos / Onix Plus",
                "Top-3 custo-benefício p/ prancha + economia (FIPE ago/2026, seguro R$2,1-2,8k/ano): Honda Fit 2020-21 (R$74-88k, Magic Seat leva prancha inteira DENTRO, mecânica mais confiável), Fiat Cronos 2022 (R$66k, 525L, 15,5 km/l, peças baratas) e Onix Plus 2021-22 (R$62-76k, consumo líder, banco 60/40 = prancha + família). Combinar com a carta de consórcio contemplada.",
                15, 83m, "baixo")
        };

        return ideas
            .Select(i => InvestmentOpportunity.Create(
                userId,
                i.Class,
                i.Title,
                i.Thesis,
                InvestmentOpportunity.SourceIdea,
                i.Risk,
                generatedAt,
                score: i.Score,
                ticker: i.Ticker,
                easeRank: i.EaseRank))
            .ToList();
    }

    /// <summary>
    /// Mapeia a classe de ativo do InvestIQ (string livre) para o AssetClass do domínio.
    /// </summary>
    private static AssetClass MapInvestIQClass(string? investIQClass)
    {
        if (string.IsNullOrWhiteSpace(investIQClass))
            return AssetClass.Outro;

        var normalized = investIQClass.Trim().ToLowerInvariant();

        var mapped = normalized switch
        {
            "acao" or "ação" or "acoes" or "ações" or "stock" or "stocks" or "equity" => AssetClass.Acao,
            "fii" or "fiis" or "reit" or "real_estate_fund" => AssetClass.Fii,
            "etf" or "etfs" => AssetClass.Etf,
            "renda_fixa" or "rendafixa" or "fixed_income" or "rf" or "bond" or "bonds" or "cdb" or "tesouro" => AssetClass.RendaFixa,
            "cripto" or "crypto" or "criptomoeda" or "cryptocurrency" => AssetClass.Cripto,
            "exterior" or "international" or "us" or "usa" or "global" => AssetClass.Exterior,
            "ouro" or "gold" => AssetClass.Ouro,
            "moeda" or "currency" or "cash" or "dolar" or "dólar" or "usd" => AssetClass.Moeda,
            "fundo" or "fund" or "fundos" => AssetClass.Fundo,
            "bdr" or "bdrs" => AssetClass.Bdr,
            "imovel" or "imóvel" or "real_estate" => AssetClass.Imovel,
            _ => (AssetClass?)null
        } ?? (Enum.TryParse<AssetClass>(investIQClass.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : AssetClass.Outro);

        return mapped;
    }

    private static OpportunityResponse MapToResponse(InvestmentOpportunity opportunity)
    {
        return new OpportunityResponse(
            opportunity.Id,
            opportunity.Class,
            opportunity.Ticker,
            opportunity.Title,
            opportunity.Thesis,
            opportunity.EaseRank,
            opportunity.Score,
            opportunity.SuggestedAllocationPct,
            opportunity.Source,
            opportunity.Risk,
            opportunity.GeneratedAt
        );
    }
}
