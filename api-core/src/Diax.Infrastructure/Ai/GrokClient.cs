using System.Net.Http.Headers;
using System.Text.Json;
using Diax.Application.AI;
using Diax.Application.AI.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class GrokClient : IGrokClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<GrokClient> _logger;

    public GrokClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GrokClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // 3-level fallback for API key
        _apiKey = configuration["XAI_API_KEY"]
            ?? configuration["Grok:ApiKey"]
            ?? configuration["PromptGenerator:Grok:ApiKey"];

        _httpClient.BaseAddress = new Uri("https://api.x.ai/v1/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<GrokModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("[Grok] API key not configured");
            return GetFallbackModels();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            _logger.LogInformation("[Grok] Fetching models list");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[Grok] API error {StatusCode}: {Error}",
                    response.StatusCode, errorBody);

                return GetFallbackModels();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<GrokModelsResponse>(json, options);

            if (result == null || result.Data == null)
            {
                _logger.LogWarning("[Grok] Invalid response format, using fallback");
                return GetFallbackModels();
            }

            // Return all models (text + image) — capability filtering is done via AiModel.CapabilitiesJson
            _logger.LogInformation("[Grok] Fetched {Count} models from API", result.Data.Count);

            return new GrokModelsResponse(result.Object, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Grok] Failed to fetch models, using fallback");
            return GetFallbackModels();
        }
    }

    private static GrokModelsResponse GetFallbackModels()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return new GrokModelsResponse(
            Object: "list",
            Data: new List<GrokModel>
            {
                new("grok-4-1-fast-reasoning", "model", now, "xai"),
                new("grok-4-1-fast-non-reasoning", "model", now, "xai"),
                new("grok-4-1-fast", "model", now, "xai"),
                new("grok-code-fast-1", "model", now, "xai"),
                new("grok-4", "model", now, "xai"),
                new("grok-4-fast", "model", now, "xai"),
                new("grok-3", "model", now, "xai"),
                new("grok-3-mini", "model", now, "xai"),
            }
        );
    }
}
