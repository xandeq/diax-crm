using System.Net.Http.Headers;
using System.Text.Json;
using Diax.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Client for OpenRouter API - specialized for model discovery
/// </summary>
public class OpenRouterClient : IOpenRouterClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterClient> _logger;
    private readonly string? _apiKey;

    public OpenRouterClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenRouterClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Read API key with fallback priority: env var > config section
        _apiKey = configuration["OPENROUTER_API_KEY"]
            ?? configuration["OpenRouter:ApiKey"]
            ?? configuration["PromptGenerator:OpenRouter:ApiKey"];

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<OpenRouterModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenRouter API key not configured");
            throw new InvalidOperationException("OpenRouter API key not configured. Set OPENROUTER_API_KEY environment variable.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            _logger.LogInformation("Fetching models from OpenRouter API");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenRouter API request failed. Status: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                throw new HttpRequestException(
                    $"OpenRouter API returned {response.StatusCode}. Check API key and network connectivity.");
            }

            var result = JsonSerializer.Deserialize<OpenRouterModelsResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Data == null)
            {
                _logger.LogWarning("OpenRouter API returned invalid response structure");
                throw new InvalidOperationException("Invalid response from OpenRouter API");
            }

            _logger.LogInformation("Successfully fetched {Count} models from OpenRouter", result.Data.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OpenRouter API request timed out");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error calling OpenRouter API");
            throw new InvalidOperationException("Failed to communicate with OpenRouter API", ex);
        }
    }
}
