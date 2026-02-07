using System.Net.Http.Headers;
using System.Text.Json;
using Diax.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Client for DeepSeek API - specialized for model discovery.
/// DeepSeek uses OpenAI-compatible API format.
/// </summary>
public class DeepSeekModelClient : IDeepSeekModelClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekModelClient> _logger;
    private readonly string? _apiKey;

    public DeepSeekModelClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DeepSeekModelClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Read API key with fallback priority: env var > config section
        _apiKey = configuration["DEEPSEEK_API_KEY"]
            ?? configuration["DeepSeek:ApiKey"]
            ?? configuration["PromptGenerator:DeepSeek:ApiKey"];

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri("https://api.deepseek.com/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<OpenAiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("DeepSeek API key not configured");
            throw new InvalidOperationException("DeepSeek API key not configured. Set DEEPSEEK_API_KEY environment variable.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("Fetching models from DeepSeek API");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "DeepSeek API request failed. Status: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                var errorMessage = (int)response.StatusCode switch
                {
                    401 => "DeepSeek API key is invalid or expired. Check DEEPSEEK_API_KEY.",
                    429 => "DeepSeek API rate limit exceeded. Try again later.",
                    >= 500 => "DeepSeek API is experiencing issues. Try again later.",
                    _ => $"DeepSeek API returned {response.StatusCode}."
                };

                throw new HttpRequestException(errorMessage);
            }

            var result = JsonSerializer.Deserialize<OpenAiModelsResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Data == null)
            {
                _logger.LogWarning("DeepSeek API returned invalid response structure");
                throw new InvalidOperationException("Invalid response from DeepSeek API");
            }

            _logger.LogInformation("Successfully fetched {Count} models from DeepSeek", result.Data.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("DeepSeek API request timed out");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error calling DeepSeek API");
            throw new InvalidOperationException("Failed to communicate with DeepSeek API", ex);
        }
    }
}
