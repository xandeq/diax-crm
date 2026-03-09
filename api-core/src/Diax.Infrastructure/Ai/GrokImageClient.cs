using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for xAI Grok Aurora.
/// Uses OpenAI-compatible /images/generations endpoint.
/// Auth: Authorization: Bearer {api_key}
/// Model: grok-2-image-1212 (free tier available)
/// Docs: https://docs.x.ai/docs/image-generation
/// </summary>
public class GrokImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GrokImageClient> _logger;

    public string ProviderName => "grok";
    public bool SupportsImageToImage => false;

    public GrokImageClient(HttpClient httpClient, ILogger<GrokImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _logger = logger;
    }

    public async Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for xAI Grok.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://api.x.ai/v1"
            : options.BaseUrl.TrimEnd('/');

        var endpoint = $"{baseUrl}/images/generations";

        var payload = new Dictionary<string, object>
        {
            ["model"] = options.Model,
            ["prompt"] = prompt ?? string.Empty,
            ["n"] = options.NumberOfImages > 0 ? options.NumberOfImages : 1,
            ["response_format"] = "url"
        };

        var json = JsonSerializer.Serialize(payload);
        _logger.LogInformation("[Grok] Submitting image generation: model={Model}", options.Model);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "[Grok] Timeout após {Timeout}s gerando imagem com modelo {Model}", _httpClient.Timeout.TotalSeconds, options.Model);
            throw new InvalidOperationException(
                $"Timeout ao gerar imagem com Grok ({_httpClient.Timeout.TotalSeconds}s). A API da xAI pode estar lenta. Tente novamente.");
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Grok] Image generation error {StatusCode}: {Body}", (int)response.StatusCode, body);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                throw new InvalidOperationException(
                    $"Rate limit da API Grok atingido. Aguarde {(int)retryAfter}s e tente novamente. Causa: {body}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new InvalidOperationException($"API key inválida ou expirada para xAI Grok. Verifique as configurações.");

            throw new InvalidOperationException(
                $"Erro ao gerar imagem com Grok. Status: {(int)response.StatusCode}. Causa: {body}");
        }

        return ParseResponse(body);
    }

    private static List<ImageGenerationResult> ParseResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();
        using var doc = JsonDocument.Parse(responseBody);

        // OpenAI-compatible: { "data": [{ "url": "...", "revised_prompt": "..." }] }
        if (!doc.RootElement.TryGetProperty("data", out var data))
            return results;

        foreach (var item in data.EnumerateArray())
        {
            var url = item.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
            var b64 = item.TryGetProperty("b64_json", out var b64Prop) ? b64Prop.GetString() : null;
            var revisedPrompt = item.TryGetProperty("revised_prompt", out var rp) ? rp.GetString() : null;

            if (!string.IsNullOrEmpty(url))
                results.Add(new ImageGenerationResult(url, false, revisedPrompt, null));
            else if (!string.IsNullOrEmpty(b64))
                results.Add(new ImageGenerationResult(b64, true, revisedPrompt, null));
        }

        return results;
    }
}
