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

    // ── Fallback: Frankfurter (base BRL → inverte) ───────────────────────────

    [Fact]
    public void ParseFrankfurter_InvertsBrlBaseRates()
    {
        // Formato real capturado 2026-07-31
        const string json = """
        {"amount":1.0,"base":"BRL","date":"2026-07-31","rates":{"EUR":0.17213,"GBP":0.1473,"USD":0.19769}}
        """;

        var (usd, eur, gbp) = MarketDataService.ParseFrankfurter(json);

        Assert.Equal(Math.Round(1m / 0.19769m, 4), usd);
        Assert.Equal(Math.Round(1m / 0.17213m, 4), eur);
        Assert.Equal(Math.Round(1m / 0.1473m, 4), gbp);
    }

    [Fact]
    public void ParseFrankfurter_MissingOrZeroRate_ReturnsNull()
    {
        const string json = """{"base":"BRL","rates":{"USD":0}}""";

        var (usd, eur, gbp) = MarketDataService.ParseFrankfurter(json);

        Assert.Null(usd);
        Assert.Null(eur);
        Assert.Null(gbp);
    }

    // ── Fallback: CoinGecko (BTC + PAXG proxy do ouro) ───────────────────────

    [Fact]
    public void ParseCoinGecko_MapsBtcAndPaxGold()
    {
        // Formato real capturado 2026-07-31
        const string json = """{"bitcoin":{"brl":318752},"pax-gold":{"brl":20599}}""";

        var (btc, goldOunce) = MarketDataService.ParseCoinGecko(json);

        Assert.Equal(318752m, btc);
        Assert.Equal(20599m, goldOunce);
    }

    [Fact]
    public void ParseCoinGecko_MissingCoin_ReturnsNull()
    {
        const string json = """{"bitcoin":{"brl":318752}}""";

        var (btc, goldOunce) = MarketDataService.ParseCoinGecko(json);

        Assert.Equal(318752m, btc);
        Assert.Null(goldOunce);
    }
}
