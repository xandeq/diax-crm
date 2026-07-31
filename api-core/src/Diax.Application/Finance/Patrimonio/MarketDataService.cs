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
/// ouro (XAU→R$/g) e BTC via AwesomeAPI — fonte gratuita, sem chave.
/// Best-effort com cache: falha da fonte nunca derruba a página.
/// </summary>
public class MarketDataService : IApplicationService
{
    private const string CacheKey = "patrimonio:market-snapshot";
    private const string AwesomeApiUrl =
        "https://economia.awesomeapi.com.br/json/last/USD-BRL,EUR-BRL,GBP-BRL,BTC-BRL,XAU-BRL";

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

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var json = await client.GetStringAsync(AwesomeApiUrl, cancellationToken);
            var snapshot = ParseAwesomeApi(json, DateTime.UtcNow);

            _cache.Set(CacheKey, snapshot, CacheTtl);
            return Result.Success(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch market snapshot from AwesomeAPI");
            return Result.Failure<MarketSnapshotResponse>(
                new Error("MarketData.Unavailable", "Market data source is unavailable right now."));
        }
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
