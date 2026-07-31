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
                14, 75m, "medio")
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
