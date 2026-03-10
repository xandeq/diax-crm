using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for Hugging Face Inference Router API.
/// Calls POST https://router.huggingface.co/hf-inference/models/{model_id}
/// Auth: Authorization: Bearer {hf_token}
/// Response: binary video (MP4/WebM) → returned as data URL
/// Supported models: damo-vilab/text-to-video-ms-1.7b, stabilityai/stable-video-diffusion-img2vid-xt, etc.
/// Docs: https://huggingface.co/docs/api-inference/tasks/text-to-video
/// Note: 503 = cold start (free tier), retry automatically with x-wait-for-model header.
/// </summary>
public class HuggingFaceVideoClient : IAiVideoGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceVideoClient> _logger;

    public string ProviderName => "huggingface";
    public bool SupportsImageToVideo => true;

    // Models that accept image input for image-to-video
    private static readonly HashSet<string> ImageToVideoModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "stabilityai/stable-video-diffusion-img2vid",
        "stabilityai/stable-video-diffusion-img2vid-xt",
        "stabilityai/stable-video-diffusion-img2vid-xt-1-1",
    };

    // Models that require structured parameters
    private static readonly HashSet<string> StructuredModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "damo-vilab/text-to-video-ms-1.7b",
        "ali-vilab/text-to-video-ms-1.7b",
        "cerspense/zeroscope_v2_576w",
        "cerspense/zeroscope_v2_xl",
        "stabilityai/stable-video-diffusion-img2vid",
        "stabilityai/stable-video-diffusion-img2vid-xt",
        "stabilityai/stable-video-diffusion-img2vid-xt-1-1",
        "THUDM/CogVideoX-2b",
        "THUDM/CogVideoX-5b",
        "THUDM/CogVideoX1.5-5b",
        "Lightricks/LTX-Video",
        "VideoCrafter/VideoCrafter1",
        "VideoCrafter/VideoCrafter2",
    };

    public HuggingFaceVideoClient(HttpClient httpClient, ILogger<HuggingFaceVideoClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(300); // Video can take several minutes on cold start
        _logger = logger;
    }

    public async Task<VideoGenerationResult> GenerateAsync(
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for Hugging Face video.");

        var modelId = options.Model;
        var endpoint = $"https://router.huggingface.co/hf-inference/models/{modelId}";

        var payload = BuildPayload(modelId, prompt, options, referenceImageBase64);
        var json = JsonSerializer.Serialize(payload);

        _logger.LogInformation("[HuggingFace Video] Generating video: model={Model}", modelId);

        // Retry up to 5 times on 503 (cold start) — common on free tier
        const int maxRetries = 5;
        const int retryDelaySeconds = 20;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            // Tell HF to wait for model to load instead of returning 503 immediately
            request.Headers.TryAddWithoutValidation("x-wait-for-model", "true");
            // HF router requires a single Accept value — multi-value causes 400
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("video/mp4"));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, ct);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "[HuggingFace Video] Timeout on attempt {Attempt}/{Max} for model {Model}", attempt, maxRetries, modelId);
                throw new InvalidOperationException(
                    $"Timeout ao gerar vídeo com HuggingFace ({_httpClient.Timeout.TotalSeconds}s). " +
                    "Modelos gratuitos podem demorar no cold start — tente novamente em instantes.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            if (response.IsSuccessStatusCode)
            {
                return await ParseSuccessResponse(response, contentType, modelId, ct);
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[HuggingFace Video] Error {StatusCode} attempt {Attempt}/{Max} for model {Model}: {Body}",
                (int)response.StatusCode, attempt, maxRetries, modelId, errorBody[..Math.Min(500, errorBody.Length)]);

            // 503: model is loading — retry with delay
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                if (attempt < maxRetries)
                {
                    string? estimatedTime = null;
                    try
                    {
                        using var errDoc = JsonDocument.Parse(errorBody);
                        if (errDoc.RootElement.TryGetProperty("estimated_time", out var et))
                            estimatedTime = $" ({et.GetDouble():F0}s estimados)";
                    }
                    catch { }

                    _logger.LogInformation(
                        "[HuggingFace Video] Model loading{EstimatedTime}, retrying in {Delay}s (attempt {Attempt}/{Max})",
                        estimatedTime ?? "", retryDelaySeconds, attempt, maxRetries);

                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), ct);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Modelo '{modelId}' está carregando no HuggingFace (cold start gratuito). " +
                    $"Tentativas esgotadas ({maxRetries}x). Aguarde alguns minutos e tente novamente.");
            }

            // 429: rate limit
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException(
                    $"Rate limit do HuggingFace atingido para o modelo '{modelId}'. " +
                    "Aguarde 60s e tente novamente.");
            }

            // 401/403: auth
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    $"API key inválida ou sem permissão para o modelo '{modelId}' no HuggingFace. " +
                    "Verifique o token em Administração > AI > Providers.");
            }

            throw new InvalidOperationException(
                $"Erro ao gerar vídeo com HuggingFace (modelo: {modelId}). " +
                $"Status: {(int)response.StatusCode}. Causa: {errorBody[..Math.Min(500, errorBody.Length)]}");
        }

        // Should not reach here
        throw new InvalidOperationException($"Falha após {maxRetries} tentativas para o modelo '{modelId}'.");
    }

    private Dictionary<string, object> BuildPayload(
        string modelId,
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64)
    {
        var payload = new Dictionary<string, object>
        {
            ["inputs"] = prompt ?? string.Empty
        };

        if (!StructuredModels.Contains(modelId))
            return payload;

        var parameters = new Dictionary<string, object>();

        // Image-to-video: pass reference image
        if (!string.IsNullOrWhiteSpace(referenceImageBase64) && ImageToVideoModels.Contains(modelId))
        {
            // SVD accepts image as base64 in parameters
            parameters["image"] = referenceImageBase64;
            _logger.LogInformation("[HuggingFace Video] img2video mode with reference image");
        }

        // Video dimensions
        if (options.Width > 0 && options.Height > 0)
        {
            parameters["width"] = options.Width;
            parameters["height"] = options.Height;
        }

        // Duration / frames (some models use num_frames instead of duration)
        if (options.DurationSeconds.HasValue)
        {
            parameters["num_frames"] = options.DurationSeconds.Value * 8; // ~8 fps default
        }

        if (!string.IsNullOrWhiteSpace(options.NegativePrompt))
            parameters["negative_prompt"] = options.NegativePrompt;

        if (parameters.Count > 0)
            payload["parameters"] = parameters;

        return payload;
    }

    private async Task<VideoGenerationResult> ParseSuccessResponse(
        HttpResponseMessage response,
        string contentType,
        string modelId,
        CancellationToken ct)
    {
        // Binary video response
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            var videoBytes = await response.Content.ReadAsByteArrayAsync(ct);
            var base64 = Convert.ToBase64String(videoBytes);
            var mimeType = contentType.Split(';')[0].Trim();
            var dataUrl = $"data:{mimeType};base64,{base64}";

            _logger.LogInformation("[HuggingFace Video] Video generated ({Bytes} bytes, type={ContentType})", videoBytes.Length, contentType);
            return new VideoGenerationResult(dataUrl, null, null);
        }

        // Some models may return JSON with a URL or base64
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return ParseJsonResponse(body, modelId);
        }

        // Fallback: treat raw bytes as MP4
        var rawBytes = await response.Content.ReadAsByteArrayAsync(ct);
        var rawBase64 = Convert.ToBase64String(rawBytes);
        var fallbackDataUrl = $"data:video/mp4;base64,{rawBase64}";
        _logger.LogWarning("[HuggingFace Video] Unknown content type '{ContentType}', treating as video/mp4", contentType);
        return new VideoGenerationResult(fallbackDataUrl, null, null);
    }

    private static VideoGenerationResult ParseJsonResponse(string body, string modelId)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // { "video": "base64..." } or { "video": { "url": "..." } }
        if (root.TryGetProperty("video", out var videoProp))
        {
            if (videoProp.ValueKind == JsonValueKind.String)
            {
                var val = videoProp.GetString() ?? "";
                // Could be base64 or URL
                if (val.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return new VideoGenerationResult(val, null, null);
                return new VideoGenerationResult($"data:video/mp4;base64,{val}", null, null);
            }
            if (videoProp.ValueKind == JsonValueKind.Object &&
                videoProp.TryGetProperty("url", out var urlProp))
            {
                return new VideoGenerationResult(urlProp.GetString() ?? "", null, null);
            }
        }

        // { "url": "..." }
        if (root.TryGetProperty("url", out var directUrl))
            return new VideoGenerationResult(directUrl.GetString() ?? "", null, null);

        throw new InvalidOperationException(
            $"HuggingFace retornou JSON sem URL de vídeo para o modelo '{modelId}': {body[..Math.Min(300, body.Length)]}");
    }
}
