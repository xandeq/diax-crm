using Diax.Application.Common;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Finance.Assets;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Calcula o resumo consolidado do patrimônio (net worth) do usuário.
/// Regras:
/// - totalRealizableBRL = soma de CurrentValue dos ativos com Liquidity != Contingente
/// - totalContingentBRL = soma de CurrentValue dos ativos com Liquidity == Contingente
/// - totalProjectedBRL = realizável + contingente
/// F1: CurrentValue é tratado como o valor equivalente em BRL informado pelo usuário.
/// </summary>
public class NetWorthService : IApplicationService
{
    private readonly IAssetRepository _repository;
    private readonly ILogger<NetWorthService> _logger;

    public NetWorthService(
        IAssetRepository repository,
        ILogger<NetWorthService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<NetWorthSummaryResponse>> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Computing net worth summary for user {UserId}", userId);
            var assets = await _repository.GetAllByUserIdAsync(userId, ct: cancellationToken);

            var realizableAssets = assets.Where(a => a.Liquidity != AssetLiquidity.Contingente).ToList();
            var contingentAssets = assets.Where(a => a.Liquidity == AssetLiquidity.Contingente).ToList();

            var totalRealizable = realizableAssets.Sum(a => a.CurrentValue);
            var totalContingent = contingentAssets.Sum(a => a.CurrentValue);
            var totalProjected = totalRealizable + totalContingent;

            // Por classe: apenas ativos realizáveis (Pct = fatia do total realizável, soma 100%)
            var byClass = realizableAssets
                .GroupBy(a => a.Class)
                .Select(g => new NetWorthByClassRow(
                    g.Key,
                    g.Sum(a => a.CurrentValue),
                    totalRealizable > 0
                        ? Math.Round(g.Sum(a => a.CurrentValue) / totalRealizable * 100m, 2)
                        : 0m))
                .OrderByDescending(r => r.TotalBRL)
                .ToList();

            // Por liquidez: todos os ativos (Contingente aparece como linha própria)
            var byLiquidity = assets
                .GroupBy(a => a.Liquidity)
                .Select(g => new NetWorthByLiquidityRow(g.Key, g.Sum(a => a.CurrentValue)))
                .OrderByDescending(r => r.TotalBRL)
                .ToList();

            // Por titularidade: todos os ativos
            var byOwnership = assets
                .GroupBy(a => a.Ownership)
                .Select(g => new NetWorthByOwnershipRow(g.Key, g.Sum(a => a.CurrentValue)))
                .OrderByDescending(r => r.TotalBRL)
                .ToList();

            // Por moeda: todos os ativos
            // F1: CurrentValue é o valor equivalente em BRL informado pelo usuário.
            // TODO: FX conversion in F3 for non-BRL currencies.
            var currencies = assets
                .GroupBy(a => a.Currency)
                .Select(g => new NetWorthCurrencyRow(g.Key, g.Sum(a => a.CurrentValue)))
                .OrderByDescending(r => r.Total)
                .ToList();

            var summary = new NetWorthSummaryResponse(
                totalRealizable,
                totalContingent,
                totalProjected,
                byClass,
                byLiquidity,
                byOwnership,
                currencies);

            _logger.LogInformation(
                "Net worth summary for user {UserId}: realizable={Realizable}, contingent={Contingent}, projected={Projected}",
                userId, totalRealizable, totalContingent, totalProjected);

            return Result<NetWorthSummaryResponse>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute net worth summary for user {UserId}", userId);
            return Result.Failure<NetWorthSummaryResponse>(
                new Error("NetWorth.QueryFailed", "Failed to compute net worth summary. Please check server logs for details."));
        }
    }
}
