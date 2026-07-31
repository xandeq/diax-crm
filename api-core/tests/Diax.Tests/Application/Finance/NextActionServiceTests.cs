using Diax.Application.Finance.Patrimonio;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

public class NextActionServiceTests
{
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IWealthProfileRepository> _wealthRepo = new();
    private readonly Mock<INextActionRepository> _actionRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public NextActionServiceTests()
    {
        _actionRepo.Setup(r => r.GetPendingByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<NextAction>());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1);
    }

    private NextActionService Build() => new(
        _assetRepo.Object,
        _wealthRepo.Object,
        _actionRepo.Object,
        _unitOfWork.Object,
        NullLogger<NextActionService>.Instance);

    private static Asset NewAsset(
        Guid userId,
        string name,
        AssetClass assetClass,
        decimal currentValue,
        AssetLiquidity liquidity = AssetLiquidity.Liquido) =>
        Asset.Create(
            userId,
            name,
            assetClass,
            AssetOwnership.Alexandre,
            liquidity,
            currentValue,
            AssetValuationSource.Manual);

    private void SetupAssets(Guid userId, params Asset[] assets)
    {
        _assetRepo.Setup(r => r.GetAllByUserIdAsync(userId, null, null, null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(assets.ToList());
        _wealthRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((WealthProfile?)null);
    }

    // ── Gap engine: subalocado → aporte ───────────────────────────────

    [Fact]
    public async Task GenerateAsync_UnderweightClass_ProducesAporteActionWithTargetClass()
    {
        var userId = Guid.NewGuid();
        // Acao = 10% do realizável vs meta default 20% → gap 10pp > margem 5pp → aporte
        SetupAssets(userId,
            NewAsset(userId, "Tesouro Selic", AssetClass.RendaFixa, 90_000m),
            NewAsset(userId, "PETR4", AssetClass.Acao, 10_000m));

        var result = await Build().GenerateAsync(userId);

        Assert.True(result.IsSuccess);
        var actions = result.Value.ToList();

        var aporteAcao = Assert.Single(actions,
            a => a.Category == NextAction.CategoryAporte && a.TargetClass == AssetClass.Acao);
        Assert.Equal(NextAction.StatusPending, aporteAcao.Status);
        Assert.NotNull(aporteAcao.SuggestedAmount);
        Assert.True(aporteAcao.SuggestedAmount > 0);
        Assert.Contains("Aportar", aporteAcao.Title);
    }

    // ── Gap engine: sobrealocado → rebalancear ────────────────────────

    [Fact]
    public async Task GenerateAsync_OverweightClass_ProducesRebalancearAction()
    {
        var userId = Guid.NewGuid();
        // RendaFixa = 90% do realizável vs meta default 25% → rebalancear
        SetupAssets(userId,
            NewAsset(userId, "Tesouro Selic", AssetClass.RendaFixa, 90_000m),
            NewAsset(userId, "PETR4", AssetClass.Acao, 10_000m));

        var result = await Build().GenerateAsync(userId);

        Assert.True(result.IsSuccess);
        var actions = result.Value.ToList();

        var rebalancear = Assert.Single(actions,
            a => a.Category == NextAction.CategoryRebalancear && a.TargetClass == AssetClass.RendaFixa);
        Assert.Contains("Rebalancear", rebalancear.Title);
    }

    // ── FGTS → alavancar imóvel ───────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_FgtsAsset_ProducesAdquirirImovelAction()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "FGTS", AssetClass.Fgts, 50_000m, AssetLiquidity.Travado));

        var result = await Build().GenerateAsync(userId);

        Assert.True(result.IsSuccess);
        var actions = result.Value.ToList();

        var fgts = Assert.Single(actions,
            a => a.Category == NextAction.CategoryAdquirir && a.TargetClass == AssetClass.Imovel);
        Assert.Equal(50_000m, fgts.SuggestedAmount);
        Assert.Contains("FGTS", fgts.Title);
    }

    // ── Ritmo da meta ─────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_BelowGoal_ProducesGoalPaceAction()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "Tesouro Selic", AssetClass.RendaFixa, 100_000m));

        var result = await Build().GenerateAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, a => a.Title.StartsWith("Faltam"));
    }
}
