using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record InvestIQAllocationItem(
    [property: JsonPropertyName("asset_class")] string AssetClass,
    [property: JsonPropertyName("total_value")] decimal TotalValue,
    [property: JsonPropertyName("percentage")] decimal Percentage
);

public record InvestIQPortfolioSummary(
    [property: JsonPropertyName("portfolio_value")] decimal PortfolioValue,
    [property: JsonPropertyName("total_invested")] decimal TotalInvested,
    [property: JsonPropertyName("unrealized_pnl")] decimal UnrealizedPnl,
    [property: JsonPropertyName("realized_pnl")] decimal RealizedPnl,
    [property: JsonPropertyName("total_return_pct")] decimal? TotalReturnPct,
    [property: JsonPropertyName("monthly_dividends")] decimal MonthlyDividends,
    [property: JsonPropertyName("position_count")] int PositionCount,
    [property: JsonPropertyName("asset_allocation")] List<InvestIQAllocationItem> AssetAllocation,
    [property: JsonPropertyName("cached_at")] string CachedAt
);

public record InvestIQOpportunityItem(
    [property: JsonPropertyName("asset_class")] string? AssetClass,
    [property: JsonPropertyName("ticker")] string? Ticker,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("thesis")] string? Thesis,
    [property: JsonPropertyName("score")] decimal Score,
    [property: JsonPropertyName("suggested_allocation_pct")] decimal? SuggestedAllocationPct,
    [property: JsonPropertyName("risk")] string? Risk
);

/// <summary>
/// Envelope real do GET /integrations/opportunities-daily do InvestIQ
/// ({"generated_at": ..., "opportunities": [...]}).
/// </summary>
public record InvestIQOpportunitiesEnvelope(
    [property: JsonPropertyName("generated_at")] string? GeneratedAt,
    [property: JsonPropertyName("opportunities")] List<InvestIQOpportunityItem>? Opportunities
);

/// <summary>
/// GET /integrations/market-snapshot do InvestIQ — cotações do dia (o VPS tem IP
/// limpo nas fontes públicas; o IP compartilhado do SmarterASP é bloqueado).
/// </summary>
public record InvestIQMarketSnapshot(
    [property: JsonPropertyName("usd_brl")] decimal? UsdBrl,
    [property: JsonPropertyName("eur_brl")] decimal? EurBrl,
    [property: JsonPropertyName("gbp_brl")] decimal? GbpBrl,
    [property: JsonPropertyName("btc_brl")] decimal? BtcBrl,
    [property: JsonPropertyName("gold_ounce_brl")] decimal? GoldOunceBrl,
    [property: JsonPropertyName("gold_gram_brl")] decimal? GoldGramBrl,
    [property: JsonPropertyName("fetched_at")] string? FetchedAt
);

// ─── Interface ───────────────────────────────────────────────────────────────

public interface IInvestIQIntegrationService
{
    Task<Result<InvestIQPortfolioSummary>> GetPortfolioSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<Result<List<InvestIQOpportunityItem>>> GetDailyOpportunitiesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<InvestIQMarketSnapshot>> GetMarketSnapshotAsync(
        CancellationToken cancellationToken = default);
}

// ─── Implementation ──────────────────────────────────────────────────────────

public class InvestIQIntegrationService : IInvestIQIntegrationService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string CacheKey = "investiq:portfolio-summary";
    private const string OpportunitiesCacheKey = "investiq:opportunities-daily";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvestIQIntegrationService> _logger;

    public InvestIQIntegrationService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<InvestIQIntegrationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<InvestIQPortfolioSummary>> GetPortfolioSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out InvestIQPortfolioSummary? cached) && cached is not null)
            return Result.Success(cached);

        var baseUrl = _configuration["InvestIQ:BaseUrl"];
        var integrationKey = _configuration["InvestIQ:IntegrationKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(integrationKey))
        {
            _logger.LogWarning("InvestIQ integration not configured (BaseUrl or IntegrationKey missing)");
            return Result.Failure<InvestIQPortfolioSummary>(
                new Error("InvestIQ.NotConfigured", "InvestIQ integration is not configured"));
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Integration-Key", integrationKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync("integrations/portfolio-summary", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("InvestIQ returned {Status}: {Body}", response.StatusCode, body);
                return Result.Failure<InvestIQPortfolioSummary>(
                    new Error("InvestIQ.Error", $"InvestIQ returned {(int)response.StatusCode}"));
            }

            var summary = await response.Content.ReadFromJsonAsync<InvestIQPortfolioSummary>(
                _json, cancellationToken);

            if (summary is null)
                return Result.Failure<InvestIQPortfolioSummary>(
                    new Error("InvestIQ.ParseError", "Failed to parse InvestIQ response"));

            _cache.Set(CacheKey, summary, CacheTtl);
            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InvestIQ integration endpoint");
            return Result.Failure<InvestIQPortfolioSummary>(
                new Error("InvestIQ.Exception", ex.Message));
        }
    }

    public async Task<Result<List<InvestIQOpportunityItem>>> GetDailyOpportunitiesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(OpportunitiesCacheKey, out List<InvestIQOpportunityItem>? cached) && cached is not null)
            return Result.Success(cached);

        var baseUrl = _configuration["InvestIQ:BaseUrl"];
        var integrationKey = _configuration["InvestIQ:IntegrationKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(integrationKey))
        {
            _logger.LogWarning("InvestIQ integration not configured (BaseUrl or IntegrationKey missing)");
            return Result.Failure<List<InvestIQOpportunityItem>>(
                new Error("InvestIQ.NotConfigured", "InvestIQ integration is not configured"));
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Integration-Key", integrationKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync("integrations/opportunities-daily", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("InvestIQ opportunities-daily returned {Status}: {Body}", response.StatusCode, body);
                return Result.Failure<List<InvestIQOpportunityItem>>(
                    new Error("InvestIQ.Error", $"InvestIQ returned {(int)response.StatusCode}"));
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var opportunities = ParseOpportunitiesPayload(payload);

            if (opportunities is null)
                return Result.Failure<List<InvestIQOpportunityItem>>(
                    new Error("InvestIQ.ParseError", "Failed to parse InvestIQ response"));

            _cache.Set(OpportunitiesCacheKey, opportunities, CacheTtl);
            return Result.Success(opportunities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InvestIQ opportunities-daily endpoint");
            return Result.Failure<List<InvestIQOpportunityItem>>(
                new Error("InvestIQ.Exception", ex.Message));
        }
    }

    public async Task<Result<InvestIQMarketSnapshot>> GetMarketSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["InvestIQ:BaseUrl"];
        var integrationKey = _configuration["InvestIQ:IntegrationKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(integrationKey))
        {
            _logger.LogWarning("InvestIQ integration not configured (BaseUrl or IntegrationKey missing)");
            return Result.Failure<InvestIQMarketSnapshot>(
                new Error("InvestIQ.NotConfigured", "InvestIQ integration is not configured"));
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Integration-Key", integrationKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync("integrations/market-snapshot", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("InvestIQ market-snapshot returned {Status}: {Body}", response.StatusCode, body);
                return Result.Failure<InvestIQMarketSnapshot>(
                    new Error("InvestIQ.Error", $"InvestIQ returned {(int)response.StatusCode}"));
            }

            var snapshot = await response.Content.ReadFromJsonAsync<InvestIQMarketSnapshot>(
                _json, cancellationToken);

            if (snapshot is null)
                return Result.Failure<InvestIQMarketSnapshot>(
                    new Error("InvestIQ.ParseError", "Failed to parse InvestIQ response"));

            return Result.Success(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling InvestIQ market-snapshot endpoint");
            return Result.Failure<InvestIQMarketSnapshot>(
                new Error("InvestIQ.Exception", ex.Message));
        }
    }

    /// <summary>
    /// O endpoint retorna um envelope {"generated_at", "opportunities": [...]}; versões
    /// antigas retornavam a lista direta. Aceita os dois formatos; null se ilegível.
    /// </summary>
    internal static List<InvestIQOpportunityItem>? ParseOpportunitiesPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            var trimmed = payload.TrimStart();
            if (trimmed.StartsWith('['))
                return JsonSerializer.Deserialize<List<InvestIQOpportunityItem>>(payload, _json);

            var envelope = JsonSerializer.Deserialize<InvestIQOpportunitiesEnvelope>(payload, _json);
            return envelope?.Opportunities;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
