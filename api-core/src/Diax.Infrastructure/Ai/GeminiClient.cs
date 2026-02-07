using System.Text.Json;
using Diax.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Client for Google Gemini API - specialized for model discovery
/// </summary>
public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string? _apiKey;

    public GeminiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Read API key with fallback priority: env var > config section
        _apiKey = configuration["GEMINI_API_KEY"]
            ?? configuration["Gemini:ApiKey"]
            ?? configuration["PromptGenerator:Gemini:ApiKey"];

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<GeminiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Gemini API key not configured");
            throw new InvalidOperationException("Gemini API key not configured. Set GEMINI_API_KEY environment variable.");
        }

        try
        {
            // Gemini uses API key as query parameter instead of Bearer token
            using var request = new HttpRequestMessage(HttpMethod.Get, $"models?pageSize=1000&key={_apiKey}");

            _logger.LogInformation("Fetching models from Gemini API");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini API request failed. Status: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                var errorMessage = (int)response.StatusCode switch
                {
                    400 => "Gemini API key is invalid. Check GEMINI_API_KEY.",
                    403 => "Gemini API key does not have permission. Check GEMINI_API_KEY.",
                    429 => "Gemini API rate limit exceeded. Try again later.",
                    >= 500 => "Gemini API is experiencing issues. Try again later.",
                    _ => $"Gemini API returned {response.StatusCode}."
                };

                throw new HttpRequestException(errorMessage);
            }

            var result = JsonSerializer.Deserialize<GeminiModelsResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Models == null)
            {
                _logger.LogWarning("Gemini API returned invalid response structure");
                throw new InvalidOperationException("Invalid response from Gemini API");
            }

            _logger.LogInformation("Successfully fetched {Count} models from Gemini", result.Models.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gemini API request timed out");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini API");
            throw new InvalidOperationException("Failed to communicate with Gemini API", ex);
        }
    }
}
