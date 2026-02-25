using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for OpenRouter — uses the OpenAI-compatible /images/generations endpoint.
/// OpenRouter proxies requests to models like FLUX, Stable Diffusion, etc.
/// </summary>
public class OpenRouterImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterImageClient> _logger;

    public string ProviderName => "openrouter";

    /// <summary>
    /// OpenRouter's /images/edits equivalent is not standardized across all proxied models.
    /// Image-to-image is therefore not guaranteed; set false to keep it safe.
    /// </summary>
    public bool SupportsImageToImage => false;

    public OpenRouterImageClient(HttpClient httpClient, ILogger<OpenRouterImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _logger = logger;
    }

    public async Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for OpenRouter.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://openrouter.ai/api/v1"
            : options.BaseUrl.TrimEnd('/');

        var endpoint = $"{baseUrl}/images/generations";

        var payload = new Dictionary<string, object>
        {
            ["model"] = options.Model,
            ["prompt"] = prompt ?? string.Empty,
            ["n"] = options.NumberOfImages,
            ["size"] = $"{options.Width}x{options.Height}",
            ["response_format"] = "url"
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        // OpenRouter requires these headers to identify the calling application
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://crm.alexandrequeiroz.com.br");
        request.Headers.TryAddWithoutValidation("X-Title", "Diax CRM");

        _logger.LogInformation(
            "[OpenRouter Image] Generating {Count} image(s) with model {Model}, size {W}x{H}",
            options.NumberOfImages, options.Model, options.Width, options.Height);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[OpenRouter Image] API error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na geração de imagem via OpenRouter. Status: {(int)response.StatusCode}. " +
                $"Verifique se o modelo '{options.Model}' suporta geração de imagens.");
        }

        return ParseOpenAiCompatibleResponse(responseBody);
    }

    private static List<ImageGenerationResult> ParseOpenAiCompatibleResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);
        var data = doc.RootElement.GetProperty("data");

        foreach (var item in data.EnumerateArray())
        {
            var url = item.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
            var b64 = item.TryGetProperty("b64_json", out var b64Prop) ? b64Prop.GetString() : null;
            var revisedPrompt = item.TryGetProperty("revised_prompt", out var rpProp) ? rpProp.GetString() : null;

            var imageUrl = url ?? (b64 != null ? $"data:image/png;base64,{b64}" : string.Empty);
            var isBase64 = url == null && b64 != null;

            results.Add(new ImageGenerationResult(
                ImageUrl: imageUrl,
                IsBase64: isBase64,
                RevisedPrompt: revisedPrompt,
                Seed: null));
        }

        return results;
    }
}
