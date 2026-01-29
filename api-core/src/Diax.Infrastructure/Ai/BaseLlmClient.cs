using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public abstract class BaseLlmClient : IAiTextTransformClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    protected BaseLlmClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public abstract string ProviderName { get; }

    public async Task<string> TransformAsync(string systemPrompt, string userPrompt, AiClientOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException($"API key not configured for provider '{ProviderName}'.");
        }

        var endpoint = BuildEndpoint(options.BaseUrl);
        var payload = new
        {
            model = options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = options.Temperature,
            max_tokens = options.MaxTokens
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI transform failed. Provider: {Provider}. Status: {StatusCode}. Response: {Response}",
                    ProviderName, (int)response.StatusCode, responseBody);
                throw new InvalidOperationException($"Falha no provedor {ProviderName}. Tente novamente mais tarde.");
            }

            var content = ExtractContent(responseBody);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException($"Resposta válida não recebida do provedor {ProviderName}.");
            }

            return content.Trim();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI transform timed out for provider {Provider}.", ProviderName);
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error calling AI provider {Provider}.", ProviderName);
            throw new InvalidOperationException($"Erro de comunicação com {ProviderName}.");
        }
    }

    private string BuildEndpoint(string baseUrl)
    {
        var cleanBase = string.IsNullOrWhiteSpace(baseUrl) ? "" : baseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(cleanBase)
            ? "chat/completions"
            : $"{cleanBase}/chat/completions";
    }

    private string ExtractContent(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
