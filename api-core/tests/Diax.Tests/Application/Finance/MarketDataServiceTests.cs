using Diax.Application.Finance.Patrimonio;

namespace Diax.Tests.Application.Finance;

public class MarketDataServiceTests
{
    private const string SampleJson = """
    {
        "USDBRL": { "code": "USD", "codein": "BRL", "bid": "5.4321" },
        "EURBRL": { "code": "EUR", "codein": "BRL", "bid": "6.1000" },
        "GBPBRL": { "code": "GBP", "codein": "BRL", "bid": "7.2500" },
        "BTCBRL": { "code": "BTC", "codein": "BRL", "bid": "634757" },
        "XAUBRL": { "code": "XAU", "codein": "BRL", "bid": "20724.00" }
    }
    """;

    [Fact]
    public void ParseAwesomeApi_FullPayload_MapsAllPairs()
    {
        var fetchedAt = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

        var snapshot = MarketDataService.ParseAwesomeApi(SampleJson, fetchedAt);

        Assert.Equal(5.4321m, snapshot.UsdBrl);
        Assert.Equal(6.1000m, snapshot.EurBrl);
        Assert.Equal(7.2500m, snapshot.GbpBrl);
        Assert.Equal(634757m, snapshot.BtcBrl);
        Assert.Equal(20724.00m, snapshot.GoldOunceBrl);
        Assert.Equal(fetchedAt, snapshot.FetchedAt);
    }

    [Fact]
    public void ParseAwesomeApi_ConvertsGoldOunceToGram()
    {
        var snapshot = MarketDataService.ParseAwesomeApi(SampleJson, DateTime.UtcNow);

        // 20724.00 / 31.1034768 = 666.2914... → arredonda pra 2 casas
        Assert.Equal(Math.Round(20724.00m / MarketDataService.TroyOunceGrams, 2), snapshot.GoldGramBrl);
        Assert.Equal(666.29m, snapshot.GoldGramBrl);
    }

    [Fact]
    public void ParseAwesomeApi_MissingPairs_ReturnsNullFields()
    {
        const string partial = """{ "USDBRL": { "bid": "5.00" } }""";

        var snapshot = MarketDataService.ParseAwesomeApi(partial, DateTime.UtcNow);

        Assert.Equal(5.00m, snapshot.UsdBrl);
        Assert.Null(snapshot.EurBrl);
        Assert.Null(snapshot.GbpBrl);
        Assert.Null(snapshot.BtcBrl);
        Assert.Null(snapshot.GoldOunceBrl);
        Assert.Null(snapshot.GoldGramBrl);
    }

    [Fact]
    public void ParseAwesomeApi_UnparseableBid_ReturnsNullField()
    {
        const string broken = """{ "USDBRL": { "bid": "not-a-number" }, "XAUBRL": { "bid": "20000" } }""";

        var snapshot = MarketDataService.ParseAwesomeApi(broken, DateTime.UtcNow);

        Assert.Null(snapshot.UsdBrl);
        Assert.Equal(20000m, snapshot.GoldOunceBrl);
        Assert.NotNull(snapshot.GoldGramBrl);
    }
}
