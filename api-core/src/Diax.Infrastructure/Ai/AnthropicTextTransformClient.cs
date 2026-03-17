using System.Net.Http.Json;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class AnthropicTextTransformClient : IAiTextTransformClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicTextTransformClient> _logger;

    public string ProviderName => "anthropic";

    public AnthropicTextTransformClient(
        HttpClient httpClient,
        ILogger<AnthropicTextTransformClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<string> TransformAsync(
        string systemPrompt,
        string userPrompt,
        AiClientOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is required");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "messages");
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = options.Model,
            max_tokens = options.MaxTokens ?? 4096,
            temperature = options.Temperature,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        request.Content = JsonContent.Create(body);

        _logger.LogInformation("[Anthropic] Sending request to model {Model}", options.Model);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[Anthropic] API error {StatusCode}: {Error}",
                response.StatusCode, errorBody);

            throw new HttpRequestException(
                $"Anthropic API returned {response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(json);

        // Anthropic response format: { "content": [{ "type": "text", "text": "..." }] }
        var text = result.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Anthropic returned empty response");
        }

        _logger.LogInformation("[Anthropic] Successfully received response");
        return text;
    }
}
