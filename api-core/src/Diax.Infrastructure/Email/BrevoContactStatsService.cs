using System.Net.Http.Json;
using System.Text.Json;
using Diax.Application.Customers.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Serviço para buscar estatísticas de email de contatos via API do Brevo.
/// Implementa cache de 24h para reduzir chamadas à API.
/// </summary>
public interface IBrevoContactStatsService
{
    Task<ContactEmailStatsResponse?> GetContactStatsAsync(string email, CancellationToken cancellationToken = default);
    Task<EmailTimelineResponse?> GetContactEmailTimelineAsync(string email, int days = 30, CancellationToken cancellationToken = default);
}

public class BrevoContactStatsService : IBrevoContactStatsService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoContactStatsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BrevoContactStatsService(
        HttpClient httpClient,
        IDistributedCache cache,
        IOptions<BrevoSettings> settings,
        ILogger<BrevoContactStatsService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri("https://api.brevo.com/v3/");
        _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
    }

    public async Task<ContactEmailStatsResponse?> GetContactStatsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var cacheKey = $"brevo:contact-stats:{email.ToLowerInvariant()}";

        // Try get from cache first
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedData))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<ContactEmailStatsResponse>(cachedData, JsonOptions);
                if (cached != null)
                {
                    _logger.LogDebug("Contact stats cache hit for {Email}", email);
                    return cached;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached contact stats for {Email}", email);
            }
        }

        // Fetch from Brevo API
        try
        {
            _logger.LogInformation("Fetching contact stats from Brevo API for {Email}", email);

            var response = await _httpClient.GetAsync(
                $"contacts/{Uri.EscapeDataString(email)}/campaignStats",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Contact not found in Brevo: {Email}", email);
                    return new ContactEmailStatsResponse { Email = email };
                }

                _logger.LogWarning(
                    "Brevo API returned {StatusCode} for contact stats: {Email}",
                    response.StatusCode, email);
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<BrevoContactStatsApiResponse>(
                JsonOptions,
                cancellationToken);

            if (apiResponse?.CampaignStats == null || apiResponse.CampaignStats.Count == 0)
            {
                _logger.LogDebug("No campaign stats found for {Email}", email);
                return new ContactEmailStatsResponse { Email = email };
            }

            // Aggregate all campaign stats
            var stats = new ContactEmailStatsResponse
            {
                Email = email,
                TotalSent = apiResponse.CampaignStats.Sum(c => c.Sent),
                TotalDelivered = apiResponse.CampaignStats.Sum(c => c.Delivered),
                TotalOpened = apiResponse.CampaignStats.Sum(c => c.Opened),
                TotalClicked = apiResponse.CampaignStats.Sum(c => c.Clicked),
                TotalBounced = apiResponse.CampaignStats.Sum(c => c.HardBounces + c.SoftBounces),
                TotalUnsubscribed = apiResponse.CampaignStats.Sum(c => c.Unsubscriptions),
                CalculatedAt = DateTime.UtcNow
            };

            // Cache for 24 hours
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(stats, JsonOptions),
                cacheOptions,
                cancellationToken);

            _logger.LogInformation(
                "Contact stats cached for {Email}: Sent={Sent}, Opened={Opened}, EngagementLevel={Level}",
                email, stats.TotalSent, stats.TotalOpened, stats.EngagementLevel);

            return stats;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching contact stats for {Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching contact stats for {Email}", email);
            return null;
        }
    }

    public async Task<EmailTimelineResponse?> GetContactEmailTimelineAsync(
        string email,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var cacheKey = $"brevo:email-timeline:{email.ToLowerInvariant()}:{days}d";

        // Try cache first
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedData))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<EmailTimelineResponse>(cachedData, JsonOptions);
                if (cached != null)
                {
                    _logger.LogDebug("Email timeline cache hit for {Email}", email);
                    return cached;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached email timeline for {Email}", email);
            }
        }

        // Fetch from Brevo API
        try
        {
            _logger.LogInformation("Fetching email timeline from Brevo API for {Email} (last {Days} days)", email, days);

            var startDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var url = $"smtp/statistics/events?email={Uri.EscapeDataString(email)}" +
                     $"&startDate={startDate}&endDate={endDate}&limit=300&sort=desc";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Brevo API returned {StatusCode} for email timeline: {Email}",
                    response.StatusCode, email);
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<BrevoEmailEventsApiResponse>(
                JsonOptions,
                cancellationToken);

            if (apiResponse?.Events == null)
            {
                _logger.LogDebug("No email events found for {Email}", email);
                return new EmailTimelineResponse { Email = email };
            }

            // Convert to timeline events
            var timeline = new EmailTimelineResponse
            {
                Email = email,
                FetchedAt = DateTime.UtcNow,
                Events = apiResponse.Events
                    .Select(e => new EmailEventDto
                    {
                        MessageId = e.MessageId,
                        Subject = e.Subject,
                        Event = ParseEventType(e.Event),
                        EventAt = DateTimeOffset.FromUnixTimeSeconds(e.Date).UtcDateTime,
                        CampaignId = ParseCampaignId(e.Tag),
                        Link = e.Link,
                        Reason = e.Reason
                    })
                    .OrderByDescending(e => e.EventAt)
                    .ToList()
            };

            // Cache for 24 hours
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(timeline, JsonOptions),
                cacheOptions,
                cancellationToken);

            _logger.LogInformation(
                "Email timeline cached for {Email}: {EventCount} events",
                email, timeline.Events.Count);

            return timeline;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching email timeline for {Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching email timeline for {Email}", email);
            return null;
        }
    }

    private static EmailEventType ParseEventType(string eventType)
    {
        return eventType?.ToLowerInvariant() switch
        {
            "request" or "sent" => EmailEventType.Sent,
            "delivered" => EmailEventType.Delivered,
            "opened" or "unique_opened" => EmailEventType.Opened,
            "click" or "unique_click" => EmailEventType.Clicked,
            "hard_bounce" or "soft_bounce" => EmailEventType.Bounced,
            "spam" or "complaint" => EmailEventType.Spam,
            "unsubscribed" => EmailEventType.Unsubscribed,
            _ => EmailEventType.Sent
        };
    }

    private static Guid? ParseCampaignId(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        return Guid.TryParse(tag, out var guid) ? guid : null;
    }
}
