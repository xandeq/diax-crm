using System.Globalization;
using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Gap engine do Patrimônio (F2): compara a alocação atual (ativos realizáveis)
/// com a alocação-alvo do WealthProfile e gera próximas ações (aporte, rebalancear,
/// FGTS → imóvel, ritmo da meta). Idempotente: substitui as ações pendentes geradas hoje.
/// </summary>
public class NextActionService : IApplicationService
{
    /// <summary>Margem (pontos percentuais) para considerar uma classe fora da meta.</summary>
    private const decimal GapMarginPct = 5m;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IAssetRepository _assetRepository;
    private readonly IWealthProfileRepository _wealthProfileRepository;
    private readonly INextActionRepository _nextActionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NextActionService> _logger;
    private readonly FinancialSummaryService? _financialSummaryService;

    public NextActionService(
        IAssetRepository assetRepository,
        IWealthProfileRepository wealthProfileRepository,
        INextActionRepository nextActionRepository,
        IUnitOfWork unitOfWork,
        ILogger<NextActionService> logger,
        FinancialSummaryService? financialSummaryService = null)
    {
        _assetRepository = assetRepository;
        _wealthProfileRepository = wealthProfileRepository;
        _nextActionRepository = nextActionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _financialSummaryService = financialSummaryService;
    }

    public async Task<Result<IEnumerable<NextActionResponse>>> GetPendingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching pending next actions for user {UserId}", userId);

            // Lazy: gera o plano do dia automaticamente na 1ª visita (se nada foi gerado hoje).
            // Torna o copiloto "fresco todo dia" sem depender de cron externo (IIS não garante timers).
            var today = DateTime.UtcNow.Date;
            var generatedToday = await _nextActionRepository.HasAnyOnOrAfterAsync(userId, today, cancellationToken);
            if (!generatedToday)
            {
                await GenerateAsync(userId, cancellationToken);
            }

            var actions = await _nextActionRepository.GetPendingByUserIdAsync(userId, cancellationToken);
            return Result<IEnumerable<NextActionResponse>>.Success(actions.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve next actions for user {UserId}", userId);
            return Result.Failure<IEnumerable<NextActionResponse>>(
                new Error("NextAction.QueryFailed", "Failed to retrieve next actions. Please check server logs for details."));
        }
    }

    /// <summary>
    /// Roda o gap engine e substitui as ações pendentes geradas hoje (idempotente).
    /// </summary>
    public async Task<Result<IEnumerable<NextActionResponse>>> GenerateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating next actions for user {UserId}", userId);

            var assets = await _assetRepository.GetAllByUserIdAsync(userId, ct: cancellationToken);
            var realizableAssets = assets.Where(a => a.Liquidity != AssetLiquidity.Contingente).ToList();
            var totalRealizable = realizableAssets.Sum(a => a.CurrentValue);

            var currentAllocation = realizableAssets
                .GroupBy(a => a.Class)
                .ToDictionary(
                    g => g.Key,
                    g => totalRealizable > 0
                        ? Math.Round(g.Sum(a => a.CurrentValue) / totalRealizable * 100m, 2)
                        : 0m);

            var profile = await _wealthProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            var goalAmount = profile?.GoalAmount ?? WealthProfileDefaults.GoalAmount;
            var goalYears = profile?.GoalYears ?? WealthProfileDefaults.GoalYears;
            var targetAllocation = WealthProfileDefaults.ToClassAllocation(
                WealthProfileDefaults.ParseAllocation(profile?.TargetAllocationJson));

            var goalMonths = Math.Max(1, goalYears * 12);
            var goalDerivedContribution = Math.Round(goalAmount / goalMonths, 2);
            var monthlyContribution = await ResolveMonthlyContributionAsync(userId, goalDerivedContribution, cancellationToken);

            var actions = new List<NextAction>();

            // ── Gap engine: subalocado → aporte / sobrealocado → rebalancear ──
            var underweight = new List<(AssetClass Class, decimal CurrentPct, decimal TargetPct, decimal Gap)>();
            foreach (var (assetClass, targetPct) in targetAllocation)
            {
                var currentPct = currentAllocation.TryGetValue(assetClass, out var pct) ? pct : 0m;

                if (currentPct < targetPct - GapMarginPct)
                {
                    underweight.Add((assetClass, currentPct, targetPct, targetPct - currentPct));
                }
                else if (currentPct > targetPct + GapMarginPct)
                {
                    actions.Add(NextAction.Create(
                        userId,
                        $"Rebalancear {ClassDisplayName(assetClass)} (atual {FormatPct(currentPct)}% > meta {FormatPct(targetPct)}%)",
                        $"A classe {ClassDisplayName(assetClass)} está {FormatPct(currentPct - targetPct)} pontos acima da meta. " +
                        "Direcione os próximos aportes para as classes subalocadas ou realize parcialmente para reequilibrar.",
                        NextAction.CategoryRebalancear,
                        targetClass: assetClass));
                }
            }

            var totalGap = underweight.Sum(u => u.Gap);
            foreach (var gap in underweight)
            {
                var suggestedAmount = totalGap > 0
                    ? Math.Round(monthlyContribution * (gap.Gap / totalGap), 2)
                    : 0m;

                actions.Add(NextAction.Create(
                    userId,
                    $"Aportar {FormatBrl(suggestedAmount)} em {ClassDisplayName(gap.Class)} (atual {FormatPct(gap.CurrentPct)}% < meta {FormatPct(gap.TargetPct)}%)",
                    $"A classe {ClassDisplayName(gap.Class)} está {FormatPct(gap.Gap)} pontos abaixo da meta de {FormatPct(gap.TargetPct)}%. " +
                    $"Sugestão proporcional ao gap sobre o aporte mensal de {FormatBrl(monthlyContribution)}.",
                    NextAction.CategoryAporte,
                    suggestedAmount: suggestedAmount,
                    targetClass: gap.Class));
            }

            // ── FGTS → alavancar imóvel ──
            var fgtsTotal = realizableAssets.Where(a => a.Class == AssetClass.Fgts).Sum(a => a.CurrentValue);
            if (fgtsTotal > 0)
            {
                actions.Add(NextAction.Create(
                    userId,
                    $"Usar FGTS de {FormatBrl(fgtsTotal)} como entrada de imóvel",
                    "O saldo do FGTS rende apenas ~3% a.a. + TR. Resgatá-lo na compra de imóvel próprio (entrada ou amortização) " +
                    "converte capital travado em patrimônio imobiliário.",
                    NextAction.CategoryAdquirir,
                    suggestedAmount: fgtsTotal,
                    targetClass: AssetClass.Imovel));
            }

            // ── Ritmo da meta ──
            var remaining = goalAmount - totalRealizable;
            if (remaining > 0)
            {
                var requiredMonthly = Math.Round(remaining / goalMonths, 2);
                actions.Add(NextAction.Create(
                    userId,
                    $"Faltam {FormatBrl(remaining)} pra meta {FormatBrl(goalAmount)} em {goalYears}a; ritmo atual {FormatBrl(monthlyContribution)}/mês vs necessário {FormatBrl(requiredMonthly)}/mês",
                    monthlyContribution >= requiredMonthly
                        ? "Ritmo atual de aporte é suficiente para atingir a meta no prazo. Mantenha a consistência."
                        : $"No ritmo atual a meta não será atingida no prazo. Aumente o aporte mensal em {FormatBrl(requiredMonthly - monthlyContribution)} ou revise a meta.",
                    NextAction.CategoryAporte,
                    suggestedAmount: requiredMonthly));
            }

            // ── Idempotência: substitui as ações pendentes geradas hoje ──
            var today = DateTime.UtcNow.Date;
            var pending = await _nextActionRepository.GetPendingByUserIdAsync(userId, cancellationToken);
            var generatedToday = pending.Where(a => a.CreatedAt >= today).ToList();
            if (generatedToday.Count > 0)
            {
                await _nextActionRepository.DeleteRangeAsync(generatedToday, cancellationToken);
            }

            await _nextActionRepository.AddRangeAsync(actions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated {Count} next actions for user {UserId} (replaced {Replaced} from today)",
                actions.Count, userId, generatedToday.Count);

            return Result<IEnumerable<NextActionResponse>>.Success(actions.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate next actions for user {UserId}", userId);
            return Result.Failure<IEnumerable<NextActionResponse>>(
                new Error("NextAction.GenerateFailed", "Failed to generate next actions. Please check server logs for details."));
        }
    }

    public async Task<Result<NextActionResponse>> CompleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Completing next action {ActionId} for user {UserId}", id, userId);
            var action = await _nextActionRepository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (action == null)
            {
                _logger.LogWarning("Next action {ActionId} not found for user {UserId}", id, userId);
                return Result.Failure<NextActionResponse>(new Error("NextAction.NotFound", "Next action not found"));
            }

            action.Complete();
            await _nextActionRepository.UpdateAsync(action, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully completed next action {ActionId} for user {UserId}", id, userId);
            return Result<NextActionResponse>.Success(MapToResponse(action));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete next action {ActionId} for user {UserId}", id, userId);
            return Result.Failure<NextActionResponse>(
                new Error("NextAction.CompleteFailed", "Failed to complete next action. Please check server logs for details."));
        }
    }

    public async Task<Result<NextActionResponse>> DismissAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dismissing next action {ActionId} for user {UserId}", id, userId);
            var action = await _nextActionRepository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (action == null)
            {
                _logger.LogWarning("Next action {ActionId} not found for user {UserId}", id, userId);
                return Result.Failure<NextActionResponse>(new Error("NextAction.NotFound", "Next action not found"));
            }

            action.Dismiss();
            await _nextActionRepository.UpdateAsync(action, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully dismissed next action {ActionId} for user {UserId}", id, userId);
            return Result<NextActionResponse>.Success(MapToResponse(action));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss next action {ActionId} for user {UserId}", id, userId);
            return Result.Failure<NextActionResponse>(
                new Error("NextAction.DismissFailed", "Failed to dismiss next action. Please check server logs for details."));
        }
    }

    /// <summary>
    /// Aporte mensal: sobra do mês corrente (FinancialSummaryService.ProjectedCashFlow)
    /// quando disponível e positiva; caso contrário, derivado da meta (GoalAmount / meses).
    /// </summary>
    private async Task<decimal> ResolveMonthlyContributionAsync(
        Guid userId,
        decimal goalDerivedContribution,
        CancellationToken cancellationToken)
    {
        if (_financialSummaryService == null)
            return goalDerivedContribution;

        try
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            var summaryResult = await _financialSummaryService.GetSummaryAsync(
                new FinancialSummaryRequest { StartDate = monthStart, EndDate = monthEnd },
                userId,
                cancellationToken);

            if (summaryResult.IsSuccess && summaryResult.Value.ProjectedCashFlow > 0)
            {
                _logger.LogInformation(
                    "Using monthly surplus {Surplus} as contribution for user {UserId}",
                    summaryResult.Value.ProjectedCashFlow, userId);
                return summaryResult.Value.ProjectedCashFlow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve monthly surplus for user {UserId}, using goal-derived contribution", userId);
        }

        return goalDerivedContribution;
    }

    private static string ClassDisplayName(AssetClass assetClass) => assetClass switch
    {
        AssetClass.RendaFixa => "Renda Fixa",
        AssetClass.Acao => "Ações",
        AssetClass.Etf => "ETFs",
        AssetClass.Fii => "FIIs",
        AssetClass.Exterior => "Exterior",
        AssetClass.Ouro => "Ouro",
        AssetClass.Cripto => "Cripto",
        AssetClass.Moeda => "Moeda estrangeira",
        AssetClass.Imovel => "Imóveis",
        AssetClass.Fgts => "FGTS",
        _ => assetClass.ToString()
    };

    private static string FormatBrl(decimal value) => "R$" + value.ToString("N0", PtBr);

    private static string FormatPct(decimal value) =>
        value == Math.Truncate(value)
            ? value.ToString("0", PtBr)
            : value.ToString("0.##", PtBr);

    private static NextActionResponse MapToResponse(NextAction action)
    {
        return new NextActionResponse(
            action.Id,
            action.Title,
            action.Rationale,
            action.Category,
            action.SuggestedAmount,
            action.TargetClass,
            action.Status,
            action.LinkedTaskId,
            action.CreatedAt
        );
    }
}
