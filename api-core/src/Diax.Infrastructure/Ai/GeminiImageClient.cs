using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for Google Gemini.
/// Supports two API modes detected by model name:
///   - "imagen-*"  → uses :predict endpoint (Imagen 3, text-to-image only)
///   - "gemini-*"  → uses :generateContent endpoint (Gemini Flash, supports image-to-image)
/// </summary>
public class GeminiImageClient : IAiImageGenerationClient
{
    private static readonly string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiImageClient> _logger;

    public string ProviderName => "gemini";

    /// <summary>
    /// Gemini Flash image generation supports image-to-image (image + prompt → edited image).
    /// </summary>
    public bool SupportsImageToImage => true;

    public GeminiImageClient(HttpClient httpClient, ILogger<GeminiImageClient> logger)
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
            throw new InvalidOperationException("API key not configured for Gemini.");

        var isImagen = options.Model.StartsWith("imagen", StringComparison.OrdinalIgnoreCase);

        if (isImagen)
        {
            _logger.LogInformation("[Gemini Image] Using Imagen API for model {Model}", options.Model);
            return await GenerateWithImagenAsync(prompt ?? string.Empty, options, ct);
        }

        _logger.LogInformation("[Gemini Image] Using Gemini generateContent API for model {Model}", options.Model);
        return await GenerateWithGeminiFlashAsync(prompt ?? string.Empty, options, referenceImageBase64, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Imagen 3: text-to-image only
    // POST /v1beta/models/{model}:predict?key={apiKey}
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<List<ImageGenerationResult>> GenerateWithImagenAsync(
        string prompt,
        ImageGenerationOptions options,
        CancellationToken ct)
    {
        var endpoint = $"{BaseUrl}/models/{options.Model}:predict?key={options.ApiKey}";

        var aspectRatio = GetAspectRatio(options.Width, options.Height);

        var payload = new
        {
            instances = new[] { new { prompt } },
            parameters = new
            {
                sampleCount = options.NumberOfImages,
                aspectRatio
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _logger.LogInformation(
            "[Gemini/Imagen] Generating {Count} image(s), aspect {Ratio}", options.NumberOfImages, aspectRatio);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Gemini/Imagen] API error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na geração de imagem via Imagen. Status: {(int)response.StatusCode}");
        }

        return ParseImagenResponse(responseBody);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gemini Flash: supports text-to-image and image-to-image
    // POST /v1beta/models/{model}:generateContent?key={apiKey}
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<List<ImageGenerationResult>> GenerateWithGeminiFlashAsync(
        string prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64,
        CancellationToken ct)
    {
        var endpoint = $"{BaseUrl}/models/{options.Model}:generateContent?key={options.ApiKey}";

        // Build parts — text first, then optional reference image
        var parts = new List<object> { new { text = prompt } };

        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            var mimeType = DetectMimeTypeFromBase64(referenceImageBase64);
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = mimeType,
                    data = referenceImageBase64
                }
            });
            _logger.LogInformation("[Gemini Flash] Image-to-image mode: reference image included (mime: {MimeType})", mimeType);
        }

        var payload = new
        {
            contents = new[]
            {
                new { parts = parts.ToArray() }
            },
            generationConfig = new
            {
                responseModalities = new[] { "TEXT", "IMAGE" }
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _logger.LogInformation("[Gemini Flash] Generating image with model {Model}", options.Model);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Gemini Flash] API error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);

            var detail = ExtractGeminiErrorDetail(responseBody);
            throw new InvalidOperationException(
                $"Falha na geração de imagem via Gemini Flash. Status: {(int)response.StatusCode}. {detail}");
        }

        return ParseGeminiFlashResponse(responseBody);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Response parsers
    // ─────────────────────────────────────────────────────────────────────────

    private static List<ImageGenerationResult> ParseImagenResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.TryGetProperty("predictions", out var predictions))
            throw new InvalidOperationException("Resposta da Imagen API não contém 'predictions'.");

        foreach (var prediction in predictions.EnumerateArray())
        {
            // Imagen returns bytesBase64Encoded
            if (prediction.TryGetProperty("bytesBase64Encoded", out var b64Prop))
            {
                var b64 = b64Prop.GetString() ?? string.Empty;
                results.Add(new ImageGenerationResult(
                    ImageUrl: $"data:image/png;base64,{b64}",
                    IsBase64: true,
                    RevisedPrompt: null,
                    Seed: null));
            }
        }

        return results;
    }

    private static List<ImageGenerationResult> ParseGeminiFlashResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
            throw new InvalidOperationException("Resposta do Gemini não contém 'candidates'.");

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content)) continue;
            if (!content.TryGetProperty("parts", out var parts)) continue;

            foreach (var part in parts.EnumerateArray())
            {
                // Look for inline image data in the response
                if (part.TryGetProperty("inlineData", out var inlineData) ||
                    part.TryGetProperty("inline_data", out inlineData))
                {
                    var mimeType = inlineData.TryGetProperty("mimeType", out var mt)
                        ? mt.GetString() ?? "image/png"
                        : inlineData.TryGetProperty("mime_type", out var mt2)
                            ? mt2.GetString() ?? "image/png"
                            : "image/png";

                    var data = inlineData.TryGetProperty("data", out var d) ? d.GetString() ?? string.Empty : string.Empty;

                    results.Add(new ImageGenerationResult(
                        ImageUrl: $"data:{mimeType};base64,{data}",
                        IsBase64: true,
                        RevisedPrompt: null,
                        Seed: null));
                }
            }
        }

        return results;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string GetAspectRatio(int width, int height)
    {
        if (width == height) return "1:1";
        if (width > height) return "16:9";
        return "9:16";
    }

    /// <summary>
    /// Detects the MIME type of an image from its base64-encoded content
    /// by inspecting the first few bytes (magic bytes).
    /// </summary>
    private static string DetectMimeTypeFromBase64(string base64)
    {
        // JPEG: starts with /9j (FFD8FF)
        if (base64.StartsWith("/9j", StringComparison.Ordinal)) return "image/jpeg";
        // PNG: starts with iVBOR (89504E47)
        if (base64.StartsWith("iVBOR", StringComparison.Ordinal)) return "image/png";
        // WEBP: starts with UklGR
        if (base64.StartsWith("UklGR", StringComparison.Ordinal)) return "image/webp";
        // GIF: starts with R0lGOD
        if (base64.StartsWith("R0lGOD", StringComparison.Ordinal)) return "image/gif";
        return "image/png"; // safe default
    }

    /// <summary>
    /// Attempts to extract a human-readable error message from a Gemini API error response body.
    /// </summary>
    private static string ExtractGeminiErrorDetail(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorObj) &&
                errorObj.TryGetProperty("message", out var msg))
            {
                var text = msg.GetString() ?? string.Empty;
                return text.Length > 300 ? text[..300] : text;
            }
        }
        catch
        {
            // ignore parse errors
        }
        return responseBody.Length > 300 ? responseBody[..300] : responseBody;
    }
}
