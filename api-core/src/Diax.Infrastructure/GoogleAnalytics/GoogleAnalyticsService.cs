using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Diax.Application.GoogleAnalytics;
using Diax.Application.GoogleAnalytics.Dtos;
using Diax.Shared.Results;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.GoogleAnalytics;

/// <summary>
/// Calls the Google Analytics Data API v1beta using a service account.
/// </summary>
public class GoogleAnalyticsService : IGoogleAnalyticsService
{
    private const string CachePrefix = "ga4:report:";
    private const string Scope = "https://www.googleapis.com/auth/analytics.readonly";
    private const string BaseUrl = "https://analyticsdata.googleapis.com/v1beta";

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleAnalyticsService> _logger;

    public GoogleAnalyticsService(
        IConfiguration configuration,
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<GoogleAnalyticsService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    // ── Configuration helpers ──────────────────────────────────────────────

    private string? PropertyId => _configuration["GA4:PropertyId"];

    private string? ServiceAccountJson =>
        _configuration["GA4:ServiceAccountJson"]
        ?? _configuration["GoogleSheets:ServiceAccountJson"]; // fallback to Sheets SA

    public Ga4StatusResponse GetStatus()
    {
        var pid = PropertyId;
        return new Ga4StatusResponse(!string.IsNullOrWhiteSpace(pid), pid);
    }

    // ── Main report ────────────────────────────────────────────────────────

    public async Task<Result<Ga4ReportResponse>> GetReportAsync(int days, CancellationToken ct = default)
    {
        var propertyId = PropertyId;
        if (string.IsNullOrWhiteSpace(propertyId))
            return Result.Failure<Ga4ReportResponse>(Error.Validation("GA4NotConfigured", "GA4 não configurado. Adicione GA4:PropertyId na configuração."));

        var saJson = ServiceAccountJson;
        if (string.IsNullOrWhiteSpace(saJson))
            return Result.Failure<Ga4ReportResponse>(Error.Validation("GA4NoCredentials", "Credenciais GA4 não encontradas. Configure GA4:ServiceAccountJson ou GoogleSheets:ServiceAccountJson."));

        var cacheKey = $"{CachePrefix}{propertyId}:{days}";
        if (_cache.TryGetValue(cacheKey, out Ga4ReportResponse? cached) && cached is not null)
            return Result<Ga4ReportResponse>.Success(cached);

        try
        {
            var token = await GetAccessTokenAsync(saJson, ct);
            var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var startDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");

            var overview = await FetchOverviewAsync(propertyId, token, startDate, endDate, ct);
            var trafficSources = await FetchTrafficSourcesAsync(propertyId, token, startDate, endDate, ct);
            var topPages = await FetchTopPagesAsync(propertyId, token, startDate, endDate, ct);
            var devices = await FetchDevicesAsync(propertyId, token, startDate, endDate, ct);
            var timeSeries = await FetchTimeSeriesAsync(propertyId, token, startDate, endDate, ct);

            var report = new Ga4ReportResponse(overview, trafficSources, topPages, devices, timeSeries, startDate, endDate);
            _cache.Set(cacheKey, report, TimeSpan.FromHours(1));
            return Result<Ga4ReportResponse>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar relatório GA4 para property {PropertyId}", propertyId);
            return Result.Failure<Ga4ReportResponse>(new Error("GA4.Error", $"Erro ao consultar GA4: {ex.Message}"));
        }
    }

    // ── Fetch methods ──────────────────────────────────────────────────────

    private async Task<Ga4OverviewMetrics> FetchOverviewAsync(
        string propertyId, string token,
        string startDate, string endDate, CancellationToken ct)
    {
        var body = new
        {
            dateRanges = new[] { new { startDate, endDate } },
            metrics = new[]
            {
                new { name = "sessions" },
                new { name = "totalUsers" },
                new { name = "newUsers" },
                new { name = "screenPageViews" },
                new { name = "bounceRate" },
                new { name = "averageSessionDuration" }
            }
        };

        var json = await RunReportAsync(propertyId, token, body, ct);
        var row = json?["rows"]?[0]?["metricValues"];
        if (row is null)
            return new Ga4OverviewMetrics(0, 0, 0, 0, 0, 0);

        return new Ga4OverviewMetrics(
            Sessions: ParseLong(row[0]),
            Users: ParseLong(row[1]),
            NewUsers: ParseLong(row[2]),
            PageViews: ParseLong(row[3]),
            BounceRate: ParseDouble(row[4]),
            AvgSessionDuration: ParseDouble(row[5]));
    }

    private async Task<List<Ga4TrafficSourceItem>> FetchTrafficSourcesAsync(
        string propertyId, string token,
        string startDate, string endDate, CancellationToken ct)
    {
        var body = new
        {
            dateRanges = new[] { new { startDate, endDate } },
            dimensions = new[] { new { name = "sessionDefaultChannelGroup" } },
            metrics = new[] { new { name = "sessions" } },
            orderBys = new[] { new { metric = new { metricName = "sessions" }, desc = true } },
            limit = 10
        };

        var json = await RunReportAsync(propertyId, token, body, ct);
        var rows = json?["rows"]?.AsArray();
        if (rows is null) return [];

        var total = rows.Sum(r => ParseLong(r?["metricValues"]?[0]));
        return rows.Select(r => new Ga4TrafficSourceItem(
            Channel: r?["dimensionValues"]?[0]?["value"]?.GetValue<string>() ?? "Unknown",
            Sessions: ParseLong(r?["metricValues"]?[0]),
            Percentage: total > 0 ? Math.Round(ParseLong(r?["metricValues"]?[0]) * 100.0 / total, 1) : 0))
            .ToList();
    }

    private async Task<List<Ga4TopPageItem>> FetchTopPagesAsync(
        string propertyId, string token,
        string startDate, string endDate, CancellationToken ct)
    {
        var body = new
        {
            dateRanges = new[] { new { startDate, endDate } },
            dimensions = new[] { new { name = "pagePath" } },
            metrics = new[]
            {
                new { name = "screenPageViews" },
                new { name = "totalUsers" }
            },
            orderBys = new[] { new { metric = new { metricName = "screenPageViews" }, desc = true } },
            limit = 10
        };

        var json = await RunReportAsync(propertyId, token, body, ct);
        var rows = json?["rows"]?.AsArray();
        if (rows is null) return [];

        return rows.Select(r => new Ga4TopPageItem(
            Page: r?["dimensionValues"]?[0]?["value"]?.GetValue<string>() ?? "/",
            PageViews: ParseLong(r?["metricValues"]?[0]),
            Users: ParseLong(r?["metricValues"]?[1])))
            .ToList();
    }

    private async Task<List<Ga4DeviceItem>> FetchDevicesAsync(
        string propertyId, string token,
        string startDate, string endDate, CancellationToken ct)
    {
        var body = new
        {
            dateRanges = new[] { new { startDate, endDate } },
            dimensions = new[] { new { name = "deviceCategory" } },
            metrics = new[] { new { name = "sessions" } },
            orderBys = new[] { new { metric = new { metricName = "sessions" }, desc = true } }
        };

        var json = await RunReportAsync(propertyId, token, body, ct);
        var rows = json?["rows"]?.AsArray();
        if (rows is null) return [];

        var total = rows.Sum(r => ParseLong(r?["metricValues"]?[0]));
        return rows.Select(r => new Ga4DeviceItem(
            Device: r?["dimensionValues"]?[0]?["value"]?.GetValue<string>() ?? "Unknown",
            Sessions: ParseLong(r?["metricValues"]?[0]),
            Percentage: total > 0 ? Math.Round(ParseLong(r?["metricValues"]?[0]) * 100.0 / total, 1) : 0))
            .ToList();
    }

    private async Task<List<Ga4TimeSeriesPoint>> FetchTimeSeriesAsync(
        string propertyId, string token,
        string startDate, string endDate, CancellationToken ct)
    {
        var body = new
        {
            dateRanges = new[] { new { startDate, endDate } },
            dimensions = new[] { new { name = "date" } },
            metrics = new[]
            {
                new { name = "sessions" },
                new { name = "totalUsers" }
            },
            orderBys = new[] { new { dimension = new { dimensionName = "date" } } }
        };

        var json = await RunReportAsync(propertyId, token, body, ct);
        var rows = json?["rows"]?.AsArray();
        if (rows is null) return [];

        return rows.Select(r =>
        {
            var raw = r?["dimensionValues"]?[0]?["value"]?.GetValue<string>() ?? "";
            var formatted = raw.Length == 8
                ? $"{raw[..4]}-{raw[4..6]}-{raw[6..]}"
                : raw;
            return new Ga4TimeSeriesPoint(
                Date: formatted,
                Sessions: ParseLong(r?["metricValues"]?[0]),
                Users: ParseLong(r?["metricValues"]?[1]));
        }).ToList();
    }

    // ── GA4 REST API call ──────────────────────────────────────────────────

    private async Task<JsonNode?> RunReportAsync(string propertyId, string token, object body, CancellationToken ct)
    {
        var url = $"{BaseUrl}/{propertyId}:runReport";
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        var rawJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GA4 API error {Status}: {Body}", response.StatusCode, rawJson);
            throw new Exception($"GA4 API retornou {(int)response.StatusCode}: {rawJson}");
        }

        return JsonNode.Parse(rawJson);
    }

    // ── Auth ───────────────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(string saJson, CancellationToken ct)
    {
        var credential = GoogleCredential
            .FromJson(saJson)
            .CreateScoped(Scope);

        var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        return token;
    }

    // ── Parse helpers ──────────────────────────────────────────────────────

    private static long ParseLong(JsonNode? node)
        => long.TryParse(node?["value"]?.GetValue<string>(), out var v) ? v : 0;

    private static double ParseDouble(JsonNode? node)
        => double.TryParse(node?["value"]?.GetValue<string>(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v) ? v : 0;
}
