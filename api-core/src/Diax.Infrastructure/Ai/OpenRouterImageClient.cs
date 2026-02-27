using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for OpenRouter.
/// Two API modes depending on the model:
///   - FLUX/SD models  → OpenAI-compatible /images/generations (text-to-image only)
///   - Gemini models   → /chat/completions with modalities:["image","text"] (supports img2img)
/// </summary>
public class OpenRouterImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterImageClient> _logger;

    public string ProviderName => "openrouter";

    /// <summary>
    /// OpenRouter supports img2img for Gemini image models via chat/completions.
    /// FLUX/SD models only support text-to-image via /images/generations.
    /// </summary>
    public bool SupportsImageToImage => true;

    public OpenRouterImageClient(HttpClient httpClient, ILogger<OpenRouterImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _logger = logger;
    }

    /// <summary>
    /// Detect if a model is a Gemini image model that uses the chat/completions API
    /// with multimodal messages (supports img2img).
    /// e.g. "google/gemini-2.5-flash-image", "google/gemini-2.0-flash-image-generation"
    /// </summary>
    private static bool IsGeminiImageModel(string model) =>
        model.StartsWith("google/gemini", StringComparison.OrdinalIgnoreCase) &&
        (model.Contains("image", StringComparison.OrdinalIgnoreCase));

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

        // Route to the correct API based on model type
        if (IsGeminiImageModel(options.Model))
        {
            return await GenerateWithGeminiAsync(prompt, options, referenceImageBase64, baseUrl, ct);
        }

        // FLUX / SD models — existing text-to-image only path
        return await GenerateWithImagesEndpointAsync(prompt, options, baseUrl, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gemini image models via /chat/completions (supports img2img)
    // POST /api/v1/chat/completions
    // Uses modalities: ["image", "text"] + multimodal messages
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<List<ImageGenerationResult>> GenerateWithGeminiAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64,
        string baseUrl,
        CancellationToken ct)
    {
        var endpoint = $"{baseUrl}/chat/completions";

        // Build multimodal message content parts
        var contentParts = new List<object>();

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            contentParts.Add(new { type = "text", text = prompt });
        }

        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            contentParts.Add(new
            {
                type = "image_url",
                image_url = new { url = $"data:image/png;base64,{referenceImageBase64}" }
            });
            _logger.LogInformation("[OpenRouter/Gemini] Image-to-image mode: reference image included");
        }

        if (contentParts.Count == 0)
        {
            contentParts.Add(new { type = "text", text = "Generate an image" });
        }

        var payload = new Dictionary<string, object>
        {
            ["model"] = options.Model,
            ["modalities"] = new[] { "image", "text" },
            ["messages"] = new[]
            {
                new
                {
                    role = "user",
                    content = contentParts.ToArray()
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://crm.alexandrequeiroz.com.br");
        request.Headers.TryAddWithoutValidation("X-Title", "Diax CRM");

        _logger.LogInformation(
            "[OpenRouter/Gemini] Generating image with model {Model}, img2img={HasRef}",
            options.Model, !string.IsNullOrWhiteSpace(referenceImageBase64));

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[OpenRouter/Gemini] API error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na geração de imagem via OpenRouter/Gemini. Status: {(int)response.StatusCode}. " +
                $"Verifique se o modelo '{options.Model}' está disponível.");
        }

        return ParseGeminiChatResponse(responseBody);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FLUX / SD models via /images/generations (text-to-image only)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<List<ImageGenerationResult>> GenerateWithImagesEndpointAsync(
        string? prompt,
        ImageGenerationOptions options,
        string baseUrl,
        CancellationToken ct)
    {
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

    // ─────────────────────────────────────────────────────────────────────────
    // Response parsers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parse Gemini chat/completions response.
    /// Gemini image models return images as base64 data URLs in the message content parts.
    /// </summary>
    private static List<ImageGenerationResult> ParseGeminiChatResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.TryGetProperty("choices", out var choices))
            return results;

        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var message))
                continue;

            if (!message.TryGetProperty("content", out var content))
                continue;

            // Content can be a string (text-only) or an array of multimodal parts
            if (content.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in content.EnumerateArray())
                {
                    var type = part.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

                    if (type == "image_url")
                    {
                        var imageUrl = part.TryGetProperty("image_url", out var imgUrlObj)
                            ? (imgUrlObj.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null)
                            : null;

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            results.Add(new ImageGenerationResult(
                                ImageUrl: imageUrl,
                                IsBase64: imageUrl.StartsWith("data:"),
                                RevisedPrompt: null,
                                Seed: null));
                        }
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Parse OpenAI-compatible /images/generations response (FLUX/SD models).
    /// </summary>
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
