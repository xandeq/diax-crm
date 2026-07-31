using Diax.Application.Finance.Patrimonio;
using Diax.Domain.Finance.Assets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

public class NetWorthServiceTests
{
    private readonly Mock<IAssetRepository> _repo = new();

    private NetWorthService Build() => new(
        _repo.Object,
        NullLogger<NetWorthService>.Instance);

    private static Asset NewAsset(
        Guid userId,
        string name,
        AssetClass assetClass,
        AssetLiquidity liquidity,
        decimal currentValue,
        AssetOwnership ownership = AssetOwnership.Alexandre,
        string? currency = null) =>
        Asset.Create(
            userId,
            name,
            assetClass,
            ownership,
            liquidity,
            currentValue,
            AssetValuationSource.Manual,
            currency);

    private void SetupAssets(Guid userId, params Asset[] assets)
    {
        _repo.Setup(r => r.GetAllByUserIdAsync(userId, null, null, null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(assets.ToList());
    }

    // ── Regra central: Contingente fora do realizável, dentro do projetado ──

    [Fact]
    public async Task GetSummaryAsync_ContingenteAsset_ExcludedFromRealizable_IncludedInProjected()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "Tesouro Selic", AssetClass.RendaFixa, AssetLiquidity.Liquido, 100_000m),
            NewAsset(userId, "Apartamento", AssetClass.Imovel, AssetLiquidity.Iliquido, 500_000m),
            NewAsset(userId, "FGTS", AssetClass.RendaFixa, AssetLiquidity.Travado, 50_000m),
            NewAsset(userId, "Precatório", AssetClass.Outro, AssetLiquidity.Contingente, 30_000m));

        var result = await Build().GetSummaryAsync(userId);

        Assert.True(result.IsSuccess);
        // Contingente (30k) NÃO entra no realizável
        Assert.Equal(650_000m, result.Value.TotalRealizableBRL);
        // Contingente aparece isolado
        Assert.Equal(30_000m, result.Value.TotalContingentBRL);
        // Projetado = realizável + contingente
        Assert.Equal(680_000m, result.Value.TotalProjectedBRL);
    }

    [Fact]
    public async Task GetSummaryAsync_OnlyContingenteAssets_RealizableIsZero()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "Ação judicial", AssetClass.Outro, AssetLiquidity.Contingente, 80_000m));

        var result = await Build().GetSummaryAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.TotalRealizableBRL);
        Assert.Equal(80_000m, result.Value.TotalContingentBRL);
        Assert.Equal(80_000m, result.Value.TotalProjectedBRL);
    }

    // ── ByClass: só realizáveis, Pct soma 100% ────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ByClass_ExcludesContingente_AndPctIsShareOfRealizable()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "PETR4", AssetClass.Acao, AssetLiquidity.Liquido, 75_000m),
            NewAsset(userId, "HGLG11", AssetClass.Fii, AssetLiquidity.Liquido, 25_000m),
            NewAsset(userId, "Herança em disputa", AssetClass.Imovel, AssetLiquidity.Contingente, 200_000m));

        var result = await Build().GetSummaryAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(100_000m, result.Value.TotalRealizableBRL);

        // Imovel (contingente) não aparece em byClass
        Assert.DoesNotContain(result.Value.ByClass, r => r.Class == AssetClass.Imovel);

        var acaoRow = Assert.Single(result.Value.ByClass, r => r.Class == AssetClass.Acao);
        Assert.Equal(75_000m, acaoRow.TotalBRL);
        Assert.Equal(75m, acaoRow.Pct);

        var fiiRow = Assert.Single(result.Value.ByClass, r => r.Class == AssetClass.Fii);
        Assert.Equal(25_000m, fiiRow.TotalBRL);
        Assert.Equal(25m, fiiRow.Pct);
    }

    // ── ByLiquidity / ByOwnership / Currencies ────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ByLiquidity_ContingenteAppearsAsOwnRow()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "Ouro", AssetClass.Ouro, AssetLiquidity.Liquido, 10_000m),
            NewAsset(userId, "Precatório", AssetClass.Outro, AssetLiquidity.Contingente, 30_000m));

        var result = await Build().GetSummaryAsync(userId);

        Assert.True(result.IsSuccess);
        var contingenteRow = Assert.Single(result.Value.ByLiquidity, r => r.Liquidity == AssetLiquidity.Contingente);
        Assert.Equal(30_000m, contingenteRow.TotalBRL);

        var liquidoRow = Assert.Single(result.Value.ByLiquidity, r => r.Liquidity == AssetLiquidity.Liquido);
        Assert.Equal(10_000m, liquidoRow.TotalBRL);
    }

    [Fact]
    public async Task GetSummaryAsync_ByOwnershipAndCurrencies_GroupAllAssets()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId,
            NewAsset(userId, "XP", AssetClass.RendaFixa, AssetLiquidity.Liquido, 18_000m, AssetOwnership.Alexandre),
            NewAsset(userId, "Poupança", AssetClass.RendaFixa, AssetLiquidity.Liquido, 5_000m, AssetOwnership.Esposa),
            NewAsset(userId, "Dólares", AssetClass.Moeda, AssetLiquidity.Liquido, 2_000m, AssetOwnership.Conjunto, "USD"));

        var result = await Build().GetSummaryAsync(userId);

        Assert.True(result.IsSuccess);

        var alexandreRow = Assert.Single(result.Value.ByOwnership, r => r.Ownership == AssetOwnership.Alexandre);
        Assert.Equal(18_000m, alexandreRow.TotalBRL);

        var usdRow = Assert.Single(result.Value.Currencies, r => r.Currency == "USD");
        Assert.Equal(2_000m, usdRow.Total);

        var brlRow = Assert.Single(result.Value.Currencies, r => r.Currency == "BRL");
        Assert.Equal(23_000m, brlRow.Total);
    }

    // ── Sem ativos ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_NoAssets_ReturnsZeroTotals()
    {
        var userId = Guid.NewGuid();
        SetupAssets(userId);

        var result = await Build().GetSummaryAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.TotalRealizableBRL);
        Assert.Equal(0m, result.Value.TotalContingentBRL);
        Assert.Equal(0m, result.Value.TotalProjectedBRL);
        Assert.Empty(result.Value.ByClass);
        Assert.Empty(result.Value.ByLiquidity);
        Assert.Empty(result.Value.ByOwnership);
        Assert.Empty(result.Value.Currencies);
    }
}
