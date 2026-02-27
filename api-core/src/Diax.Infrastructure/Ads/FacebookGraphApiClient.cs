using Diax.Application.Ads.Dtos;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Diax.Infrastructure.Ads;

/// <summary>
/// Cliente HTTP para a Facebook Graph API v21.0.
/// </summary>
public class FacebookGraphApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FacebookGraphApiClient> _logger;
    private const string GraphApiBase = "https://graph.facebook.com/v21.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FacebookGraphApiClient(HttpClient httpClient, ILogger<FacebookGraphApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ===== ACCOUNT =====

    public async Task<FacebookAccountInfoResult?> GetAccountInfoAsync(string adAccountId, string accessToken, CancellationToken ct = default)
    {
        var fields = "name,currency,timezone_name,account_status";
        var url = $"{GraphApiBase}/{adAccountId}?fields={fields}&access_token={accessToken}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook API error getting account info: {Content}", content);
                return null;
            }

            return JsonSerializer.Deserialize<FacebookAccountInfoResult>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Facebook Graph API for account info");
            return null;
        }
    }

    // ===== CAMPAIGNS =====

    public async Task<List<FacebookCampaignDto>> GetCampaignsAsync(string adAccountId, string accessToken, CancellationToken ct = default)
    {
        var fields = "id,name,status,objective,budget_remaining,daily_budget,lifetime_budget,start_time,stop_time,created_time,updated_time";
        var url = $"{GraphApiBase}/{adAccountId}/campaigns?fields={fields}&limit=100&access_token={accessToken}";

        return await FetchPaginatedAsync<FacebookCampaignResult, FacebookCampaignDto>(
            url, accessToken, MapCampaign, ct);
    }

    // ===== AD SETS =====

    public async Task<List<FacebookAdSetDto>> GetAdSetsAsync(string adAccountId, string accessToken, string? campaignId = null, CancellationToken ct = default)
    {
        var fields = "id,campaign_id,name,status,daily_budget,lifetime_budget,billing_event,optimization_goal,start_time,end_time,created_time,updated_time";
        var endpoint = campaignId != null
            ? $"{GraphApiBase}/{campaignId}/adsets"
            : $"{GraphApiBase}/{adAccountId}/adsets";
        var url = $"{endpoint}?fields={fields}&limit=100&access_token={accessToken}";

        return await FetchPaginatedAsync<FacebookAdSetResult, FacebookAdSetDto>(
            url, accessToken, MapAdSet, ct);
    }

    // ===== ADS =====

    public async Task<List<FacebookAdDto>> GetAdsAsync(string adAccountId, string accessToken, string? adSetId = null, CancellationToken ct = default)
    {
        var fields = "id,adset_id,campaign_id,name,status,creative,created_time,updated_time";
        var endpoint = adSetId != null
            ? $"{GraphApiBase}/{adSetId}/ads"
            : $"{GraphApiBase}/{adAccountId}/ads";
        var url = $"{endpoint}?fields={fields}&limit=100&access_token={accessToken}";

        return await FetchPaginatedAsync<FacebookAdResult, FacebookAdDto>(
            url, accessToken, MapAd, ct);
    }

    // ===== INSIGHTS =====

    public async Task<List<FacebookInsightDto>> GetInsightsAsync(
        string adAccountId,
        string accessToken,
        InsightsRequest request,
        CancellationToken ct = default)
    {
        var fields = "campaign_id,campaign_name,adset_id,adset_name,ad_id,ad_name,date_start,date_stop,impressions,clicks,spend,reach,ctr,cpc,cpm,cpp,frequency,actions,conversions";
        var dateParam = !string.IsNullOrWhiteSpace(request.Since) && !string.IsNullOrWhiteSpace(request.Until)
            ? $"&time_range={{\"since\":\"{request.Since}\",\"until\":\"{request.Until}\"}}"
            : $"&date_preset={request.DatePreset ?? "last_30d"}";

        var url = $"{GraphApiBase}/{adAccountId}/insights?fields={fields}&level={request.Level}&limit=100{dateParam}&access_token={accessToken}";

        return await FetchPaginatedAsync<FacebookInsightResult, FacebookInsightDto>(
            url, accessToken, MapInsight, ct);
    }

    // ===== PAGINATION HELPER =====

    private async Task<List<TDto>> FetchPaginatedAsync<TRaw, TDto>(
        string initialUrl,
        string accessToken,
        Func<TRaw, TDto> mapper,
        CancellationToken ct) where TRaw : class
    {
        var results = new List<TDto>();
        var nextUrl = initialUrl;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            try
            {
                var response = await _httpClient.GetAsync(nextUrl, ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Facebook API error: {Content}", content);
                    break;
                }

                var page = JsonSerializer.Deserialize<FacebookPaginatedResponse<TRaw>>(content, JsonOptions);
                if (page?.Data == null) break;

                results.AddRange(page.Data.Select(mapper));

                nextUrl = page.Paging?.Next;
                if (!string.IsNullOrEmpty(nextUrl) && nextUrl == initialUrl) break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated data from Facebook API");
                break;
            }
        }

        return results;
    }

    // ===== MAPPERS =====

    private static FacebookCampaignDto MapCampaign(FacebookCampaignResult r) => new(
        r.Id ?? "",
        r.Name ?? "",
        r.Status ?? "",
        r.Objective ?? "",
        r.BudgetRemaining,
        r.DailyBudget,
        r.LifetimeBudget,
        r.StartTime,
        r.StopTime,
        r.CreatedTime ?? DateTime.MinValue,
        r.UpdatedTime ?? DateTime.MinValue);

    private static FacebookAdSetDto MapAdSet(FacebookAdSetResult r) => new(
        r.Id ?? "",
        r.CampaignId ?? "",
        r.Name ?? "",
        r.Status ?? "",
        r.DailyBudget,
        r.LifetimeBudget,
        r.BillingEvent ?? "",
        r.OptimizationGoal ?? "",
        r.StartTime,
        r.EndTime,
        r.CreatedTime ?? DateTime.MinValue,
        r.UpdatedTime ?? DateTime.MinValue);

    private static FacebookAdDto MapAd(FacebookAdResult r) => new(
        r.Id ?? "",
        r.AdsetId ?? "",
        r.CampaignId ?? "",
        r.Name ?? "",
        r.Status ?? "",
        r.Creative?.Id,
        r.CreatedTime ?? DateTime.MinValue,
        r.UpdatedTime ?? DateTime.MinValue);

    private static FacebookInsightDto MapInsight(FacebookInsightResult r) => new(
        r.CampaignId ?? "",
        r.CampaignName,
        r.AdsetId,
        r.AdsetName,
        r.AdId,
        r.AdName,
        r.DateStart ?? "",
        r.DateStop ?? "",
        r.Impressions ?? "0",
        r.Clicks ?? "0",
        r.Spend ?? "0",
        r.Reach ?? "0",
        r.Ctr,
        r.Cpc,
        r.Cpm,
        r.Cpp,
        r.Frequency,
        r.Actions?.Select(a => new FacebookActionDto(a.ActionType ?? "", a.Value ?? "0")).ToList() ?? [],
        r.Conversions?.Select(a => new FacebookActionDto(a.ActionType ?? "", a.Value ?? "0")).ToList() ?? []);
}

// ===== RAW RESPONSE MODELS =====

public class FacebookPaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public List<T>? Data { get; set; }

    [JsonPropertyName("paging")]
    public FacebookPaging? Paging { get; set; }
}

public class FacebookPaging
{
    [JsonPropertyName("cursors")]
    public FacebookCursors? Cursors { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public class FacebookCursors
{
    [JsonPropertyName("before")]
    public string? Before { get; set; }

    [JsonPropertyName("after")]
    public string? After { get; set; }
}

public class FacebookAccountInfoResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("timezone_name")]
    public string? TimezoneName { get; set; }

    [JsonPropertyName("account_status")]
    public int AccountStatus { get; set; }
}

public class FacebookCampaignResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("budget_remaining")]
    public string? BudgetRemaining { get; set; }

    [JsonPropertyName("daily_budget")]
    public string? DailyBudget { get; set; }

    [JsonPropertyName("lifetime_budget")]
    public string? LifetimeBudget { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("stop_time")]
    public DateTime? StopTime { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime? CreatedTime { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime? UpdatedTime { get; set; }
}

public class FacebookAdSetResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("campaign_id")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("daily_budget")]
    public string? DailyBudget { get; set; }

    [JsonPropertyName("lifetime_budget")]
    public string? LifetimeBudget { get; set; }

    [JsonPropertyName("billing_event")]
    public string? BillingEvent { get; set; }

    [JsonPropertyName("optimization_goal")]
    public string? OptimizationGoal { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime? CreatedTime { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime? UpdatedTime { get; set; }
}

public class FacebookAdResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("adset_id")]
    public string? AdsetId { get; set; }

    [JsonPropertyName("campaign_id")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("creative")]
    public FacebookCreativeRef? Creative { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime? CreatedTime { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime? UpdatedTime { get; set; }
}

public class FacebookCreativeRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class FacebookInsightResult
{
    [JsonPropertyName("campaign_id")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("campaign_name")]
    public string? CampaignName { get; set; }

    [JsonPropertyName("adset_id")]
    public string? AdsetId { get; set; }

    [JsonPropertyName("adset_name")]
    public string? AdsetName { get; set; }

    [JsonPropertyName("ad_id")]
    public string? AdId { get; set; }

    [JsonPropertyName("ad_name")]
    public string? AdName { get; set; }

    [JsonPropertyName("date_start")]
    public string? DateStart { get; set; }

    [JsonPropertyName("date_stop")]
    public string? DateStop { get; set; }

    [JsonPropertyName("impressions")]
    public string? Impressions { get; set; }

    [JsonPropertyName("clicks")]
    public string? Clicks { get; set; }

    [JsonPropertyName("spend")]
    public string? Spend { get; set; }

    [JsonPropertyName("reach")]
    public string? Reach { get; set; }

    [JsonPropertyName("ctr")]
    public string? Ctr { get; set; }

    [JsonPropertyName("cpc")]
    public string? Cpc { get; set; }

    [JsonPropertyName("cpm")]
    public string? Cpm { get; set; }

    [JsonPropertyName("cpp")]
    public string? Cpp { get; set; }

    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    [JsonPropertyName("actions")]
    public List<FacebookActionResult>? Actions { get; set; }

    [JsonPropertyName("conversions")]
    public List<FacebookActionResult>? Conversions { get; set; }
}

public class FacebookActionResult
{
    [JsonPropertyName("action_type")]
    public string? ActionType { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
