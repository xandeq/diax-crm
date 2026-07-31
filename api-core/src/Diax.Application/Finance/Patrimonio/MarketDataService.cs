using System.Globalization;
using System.Text.Json;
using Diax.Application.Common;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Cotações live para o copiloto de patrimônio (F3): moeda forte (USD/EUR/GBP),
/// ouro (XAU→R$/g) e BTC. Fontes gratuitas sem chave, com fallback:
/// AwesomeAPI (tudo em 1 chamada) → Frankfurter (câmbio ECB) + CoinGecko
/// (BTC e PAXG como proxy da onça de ouro). O IP compartilhado do SmarterASP
/// vive rate-limitado (429) na AwesomeAPI — o fallback é o caminho comum em prod.
/// Best-effort com cache: falha total das fontes nunca derruba a página.
/// </summary>
public class MarketDataService : IApplicationService
{
    private const string CacheKey = "patrimonio:market-snapshot";
    private const string AwesomeApiUrl =
        "https://economia.awesomeapi.com.br/json/last/USD-BRL,EUR-BRL,GBP-BRL,BTC-BRL,XAU-BRL";
    private const string FrankfurterUrl =
        "https://api.frankfurter.dev/v1/latest?base=BRL&symbols=USD,EUR,GBP";
    private const string CoinGeckoUrl =
        "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,pax-gold&vs_currencies=brl";

    /// <summary>Gramas em uma onça troy — XAU é cotado por onça, joia/barra se negocia por grama.</summary>
    internal const decimal TroyOunceGrams = 31.1034768m;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<MarketDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<MarketSnapshotResponse>> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out MarketSnapshotResponse? cached) && cached is not null)
            return Result.Success(cached);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            var json = await client.GetStringAsync(AwesomeApiUrl, cancellationToken);
            var snapshot = ParseAwesomeApi(json, DateTime.UtcNow);

            _cache.Set(CacheKey, snapshot, CacheTtl);
            return Result.Success(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AwesomeAPI unavailable, falling back to Frankfurter + CoinGecko");
        }

        var fallback = await GetFallbackSnapshotAsync(client, cancellationToken);
        if (fallback is not null)
        {
            _cache.Set(CacheKey, fallback, CacheTtl);
            return Result.Success(fallback);
        }

        return Result.Failure<MarketSnapshotResponse>(
            new Error("MarketData.Unavailable", "Market data sources are unavailable right now."));
    }

    /// <summary>
    /// Fallback em duas fontes independentes, cada uma best-effort: snapshot
    /// parcial vale (o front só pinta os campos preenchidos). Null se ambas falharem.
    /// </summary>
    private async Task<MarketSnapshotResponse?> GetFallbackSnapshotAsync(
        HttpClient client, CancellationToken cancellationToken)
    {
        (decimal? usd, decimal? eur, decimal? gbp) fx = (null, null, null);
        (decimal? btc, decimal? goldOunce) crypto = (null, null);

        try
        {
            fx = ParseFrankfurter(await client.GetStringAsync(FrankfurterUrl, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Frankfurter fallback failed");
        }

        try
        {
            crypto = ParseCoinGecko(await client.GetStringAsync(CoinGeckoUrl, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CoinGecko fallback failed");
        }

        if (fx is (null, null, null) && crypto is (null, null))
            return null;

        return new MarketSnapshotResponse(
            UsdBrl: fx.usd,
            EurBrl: fx.eur,
            GbpBrl: fx.gbp,
            BtcBrl: crypto.btc,
            GoldOunceBrl: crypto.goldOunce,
            GoldGramBrl: crypto.goldOunce.HasValue
                ? Math.Round(crypto.goldOunce.Value / TroyOunceGrams, 2)
                : null,
            FetchedAt: DateTime.UtcNow);
    }

    /// <summary>
    /// Parse do payload da AwesomeAPI (objeto com chaves "USDBRL", "XAUBRL", ...,
    /// cada uma com "bid" string). Par ausente/ilegível vira campo nulo — nunca lança.
    /// </summary>
    internal static MarketSnapshotResponse ParseAwesomeApi(string json, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var goldOunce = ReadBid(root, "XAUBRL");

        return new MarketSnapshotResponse(
            UsdBrl: ReadBid(root, "USDBRL"),
            EurBrl: ReadBid(root, "EURBRL"),
            GbpBrl: ReadBid(root, "GBPBRL"),
            BtcBrl: ReadBid(root, "BTCBRL"),
            GoldOunceBrl: goldOunce,
            GoldGramBrl: goldOunce.HasValue
                ? Math.Round(goldOunce.Value / TroyOunceGrams, 2)
                : null,
            FetchedAt: fetchedAt);
    }

    /// <summary>
    /// Frankfurter com base=BRL retorna quanto 1 BRL vale na moeda ({"rates":{"USD":0.19769,...}}) —
    /// o par X-BRL é o inverso (1/rate). Rate ausente/zero vira null.
    /// </summary>
    internal static (decimal? Usd, decimal? Eur, decimal? Gbp) ParseFrankfurter(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("rates", out var rates))
            return (null, null, null);

        return (Invert(rates, "USD"), Invert(rates, "EUR"), Invert(rates, "GBP"));

        static decimal? Invert(JsonElement rates, string symbol)
        {
            if (!rates.TryGetProperty(symbol, out var el) || !el.TryGetDecimal(out var rate) || rate <= 0)
                return null;
            return Math.Round(1m / rate, 4);
        }
    }

    /// <summary>
    /// CoinGecko simple/price: {"bitcoin":{"brl":318752},"pax-gold":{"brl":20599}}.
    /// PAXG é lastreado em 1 onça troy de ouro — serve de proxy da cotação XAU-BRL.
    /// </summary>
    internal static (decimal? Btc, decimal? GoldOunce) ParseCoinGecko(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return (ReadBrl(doc.RootElement, "bitcoin"), ReadBrl(doc.RootElement, "pax-gold"));

        static decimal? ReadBrl(JsonElement root, string id)
        {
            if (!root.TryGetProperty(id, out var coin) || !coin.TryGetProperty("brl", out var el))
                return null;
            return el.TryGetDecimal(out var value) ? value : null;
        }
    }

    private static decimal? ReadBid(JsonElement root, string pairKey)
    {
        if (!root.TryGetProperty(pairKey, out var pair))
            return null;
        if (!pair.TryGetProperty("bid", out var bid))
            return null;

        var raw = bid.ValueKind == JsonValueKind.String ? bid.GetString() : bid.GetRawText();
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
