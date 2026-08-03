using Diax.Application.Finance.Patrimonio;
using Diax.Domain.Finance.Assets;
using Xunit;

namespace Diax.Tests.Application.Finance;

public class FipeServiceTests
{
    // ===== ParsePrice (moeda pt-BR da FIPE) =====

    [Theory]
    [InlineData("R$ 135.836,00", 135836.00)]
    [InlineData("R$ 1.234.567,89", 1234567.89)]
    [InlineData("R$ 950,50", 950.50)]
    public void ParsePrice_BrazilianCurrency_ReturnsDecimal(string raw, decimal expected)
    {
        Assert.Equal(expected, FipeService.ParsePrice(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("R$ 0,00")]
    public void ParsePrice_InvalidOrZero_ReturnsNull(string? raw)
    {
        Assert.Null(FipeService.ParsePrice(raw));
    }

    // ===== ParseCatalog =====

    [Fact]
    public void ParseCatalog_ValidArray_ReturnsItems()
    {
        const string json = """
        [{"code":"56","name":"Toyota"},{"code":"59","name":"VW - VolksWagen"}]
        """;

        var items = FipeService.ParseCatalog(json);

        Assert.Equal(2, items.Count);
        Assert.Equal("56", items[0].Code);
        Assert.Equal("Toyota", items[0].Name);
    }

    [Fact]
    public void ParseCatalog_SkipsUnreadableItems()
    {
        const string json = """
        [{"code":"56","name":"Toyota"},{"code":57},{"name":"SemCodigo"}]
        """;

        var items = FipeService.ParseCatalog(json);

        Assert.Single(items);
        Assert.Equal("Toyota", items[0].Name);
    }

    [Fact]
    public void ParseCatalog_NonArray_ReturnsEmpty()
    {
        Assert.Empty(FipeService.ParseCatalog("""{"error":"not found"}"""));
    }

    // ===== ParsePriceJson (payload real Parallelum v2) =====

    [Fact]
    public void ParsePriceJson_RealPayload_MapsAllFields()
    {
        const string json = """
        {
            "vehicleType": 1,
            "price": "R$ 135.836,00",
            "brand": "Toyota",
            "model": "Corolla Cross XRE 2.0 16V Flex Aut.",
            "modelYear": 2023,
            "fuel": "Flex",
            "codeFipe": "002203-9",
            "referenceMonth": "agosto de 2026",
            "fuelAcronym": "F"
        }
        """;

        var price = FipeService.ParsePriceJson(json);

        Assert.NotNull(price);
        Assert.Equal(135836.00m, price!.Price);
        Assert.Equal("Toyota", price.Brand);
        Assert.Equal("Corolla Cross XRE 2.0 16V Flex Aut.", price.Model);
        Assert.Equal(2023, price.ModelYear);
        Assert.Equal("Flex", price.Fuel);
        Assert.Equal("002203-9", price.CodeFipe);
        Assert.Equal("agosto de 2026", price.ReferenceMonth);
    }

    [Fact]
    public void ParsePriceJson_MissingPrice_ReturnsNull()
    {
        Assert.Null(FipeService.ParsePriceJson("""{"brand":"Toyota","modelYear":2023}"""));
    }

    // ===== Domínio: vínculo FIPE no Asset =====

    private static Asset NewVehicle() => Asset.Create(
        Guid.NewGuid(), "Corolla Cross", AssetClass.Veiculo, AssetOwnership.Alexandre,
        AssetLiquidity.Iliquido, 180_000m, AssetValuationSource.Manual);

    [Fact]
    public void LinkFipe_Vehicle_SetsCodesAndFipeSource()
    {
        var asset = NewVehicle();

        asset.LinkFipe("CARS", "56", "9326", "2023-5");

        Assert.True(asset.HasFipeLink);
        Assert.Equal("cars", asset.FipeVehicleType);
        Assert.Equal("56", asset.FipeBrandCode);
        Assert.Equal("9326", asset.FipeModelCode);
        Assert.Equal("2023-5", asset.FipeYearCode);
        Assert.Equal(AssetValuationSource.Fipe, asset.ValuationSource);
    }

    [Fact]
    public void LinkFipe_NonVehicle_Throws()
    {
        var gold = Asset.Create(
            Guid.NewGuid(), "Ouro", AssetClass.Ouro, AssetOwnership.Alexandre,
            AssetLiquidity.Liquido, 10_000m, AssetValuationSource.Market);

        Assert.Throws<ArgumentException>(() => gold.LinkFipe("cars", "56", "9326", "2023-5"));
    }

    [Fact]
    public void LinkFipe_MissingCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewVehicle().LinkFipe("cars", "56", "", "2023-5"));
    }

    [Fact]
    public void UnlinkFipe_ClearsCodesAndFallsBackToManual()
    {
        var asset = NewVehicle();
        asset.LinkFipe("cars", "56", "9326", "2023-5");

        asset.UnlinkFipe();

        Assert.False(asset.HasFipeLink);
        Assert.Null(asset.FipeVehicleType);
        Assert.Equal(AssetValuationSource.Manual, asset.ValuationSource);
    }
}
