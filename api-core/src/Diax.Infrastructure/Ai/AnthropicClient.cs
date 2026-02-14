using System.Text.Json;
using Diax.Application.AI;
using Diax.Application.AI.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class AnthropicClient : IAnthropicClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<AnthropicClient> _logger;

    public AnthropicClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnthropicClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // 3-level fallback for API key
        _apiKey = configuration["ANTHROPIC_API_KEY"]
            ?? configuration["Anthropic:ApiKey"]
            ?? configuration["PromptGenerator:Anthropic:ApiKey"];

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<AnthropicModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("[Anthropic] API key not configured");

            // Fallback: Return hardcoded models if API key is missing
            return GetFallbackModels();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            _logger.LogInformation("[Anthropic] Fetching models list");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[Anthropic] API error {StatusCode}: {Error}",
                    response.StatusCode, errorBody);

                // Fallback to hardcoded list on error
                return GetFallbackModels();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<AnthropicModelsResponse>(json, options);

            if (result == null || result.Data == null)
            {
                _logger.LogWarning("[Anthropic] Invalid response format, using fallback");
                return GetFallbackModels();
            }

            _logger.LogInformation("[Anthropic] Fetched {Count} models from API", result.Data.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Anthropic] Failed to fetch models, using fallback");
            return GetFallbackModels();
        }
    }

    private static AnthropicModelsResponse GetFallbackModels()
    {
        return new AnthropicModelsResponse(
            Data: new List<AnthropicModel>
            {
                new("claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet (New)", DateTime.UtcNow, "model"),
                new("claude-3-5-haiku-20241022", "Claude 3.5 Haiku", DateTime.UtcNow, "model"),
                new("claude-3-opus-20240229", "Claude 3 Opus", new DateTime(2024, 2, 29), "model"),
                new("claude-3-sonnet-20240229", "Claude 3 Sonnet", new DateTime(2024, 2, 29), "model"),
                new("claude-3-haiku-20240307", "Claude 3 Haiku", new DateTime(2024, 3, 7), "model"),
            },
            HasMore: false,
            FirstId: "claude-3-5-sonnet-20241022",
            LastId: "claude-3-haiku-20240307"
        );
    }
}
