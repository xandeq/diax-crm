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

// ─── Interface ───────────────────────────────────────────────────────────────

public interface IInvestIQIntegrationService
{
    Task<Result<InvestIQPortfolioSummary>> GetPortfolioSummaryAsync(
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
}
