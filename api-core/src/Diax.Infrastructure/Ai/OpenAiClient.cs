using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Client for OpenAI API - specialized for model discovery
/// </summary>
public class OpenAiClient : IOpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiClient> _logger;
    private readonly string? _apiKey;

    public OpenAiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Read API key with fallback priority: env var > config section
        _apiKey = configuration["OPENAI_API_KEY"]
            ?? configuration["OpenAI:ApiKey"]
            ?? configuration["PromptGenerator:OpenAI:ApiKey"];

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<OpenAiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenAI API key not configured");
            throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("Fetching models from OpenAI API");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI API request failed. Status: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                var errorMessage = (int)response.StatusCode switch
                {
                    401 => "OpenAI API key is invalid or expired. Check OPENAI_API_KEY.",
                    429 => "OpenAI API rate limit exceeded. Try again later.",
                    >= 500 => "OpenAI API is experiencing issues. Try again later.",
                    _ => $"OpenAI API returned {response.StatusCode}."
                };

                throw new HttpRequestException(errorMessage);
            }

            var result = JsonSerializer.Deserialize<OpenAiModelsResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Data == null)
            {
                _logger.LogWarning("OpenAI API returned invalid response structure");
                throw new InvalidOperationException("Invalid response from OpenAI API");
            }

            _logger.LogInformation("Successfully fetched {Count} models from OpenAI", result.Data.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OpenAI API request timed out");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not HttpRequestException)
        {
            _logger.LogError(ex, "Unexpected error calling OpenAI API");
            throw new InvalidOperationException("Failed to communicate with OpenAI API", ex);
        }
    }

    public async Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(OpenAiChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenAI API key not configured");
            throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            httpRequest.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending chat completion request to OpenAI model {Model}", request.Model);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI API request failed. Status: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                throw new HttpRequestException($"OpenAI API returned {response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || result.Choices == null)
            {
                _logger.LogWarning("OpenAI API returned invalid response structure");
                throw new InvalidOperationException("Invalid response from OpenAI API");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat completion");
            throw;
        }
    }
}
