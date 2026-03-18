using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for fal.ai video models (Kling, WAN, Luma, Minimax, CogVideoX).
/// Uses the Queue API: POST to queue.fal.run/{model_id}, then polls status via GET.
/// Auth: Authorization: Key {api_key}  (NOT Bearer)
/// Docs: https://fal.ai/docs/model-endpoints/queue
/// </summary>
public class FalAiVideoClient : IAiVideoGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FalAiVideoClient> _logger;

    public string ProviderName => "falai";
    public bool SupportsImageToVideo => true;

    /// <summary>
    /// Models that accept image_url as input (image-to-video).
    /// </summary>
    private static readonly HashSet<string> ImageToVideoModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "fal-ai/kling-video/v2.1/standard/image-to-video",
        "fal-ai/kling-video/v1.6/pro/image-to-video",
        "fal-ai/kling-video/v1.5/pro/image-to-video",
        "fal-ai/wan/v1.3/image-to-video",
        "fal-ai/wan-i2v",
        "fal-ai/luma-dream-machine/image-to-video",
        "fal-ai/hunyuan-video-image-to-video",
        "fal-ai/ltx-video/image-to-video",
        "fal-ai/pika/v2.2/image-to-video",
        "fal-ai/pika-labs/v2.2/image-to-video",
        "fal-ai/cogvideox-5b-img2vid",
    };

    public FalAiVideoClient(HttpClient httpClient, ILogger<FalAiVideoClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public async Task<VideoGenerationResult> GenerateAsync(
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for fal.ai video.");

        var modelId = options.Model;
        var submitEndpoint = $"https://queue.fal.run/{modelId}";

        var inputPayload = new Dictionary<string, object>
        {
            ["prompt"] = prompt ?? string.Empty
        };

        // Image-to-video: pass reference image as data URL
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            inputPayload["image_url"] = $"data:image/png;base64,{referenceImageBase64}";
            _logger.LogInformation("[Fal.ai Video] img2video mode with reference image");
        }

        // Duration (some models accept duration_seconds or duration)
        if (options.DurationSeconds.HasValue)
        {
            inputPayload["duration"] = options.DurationSeconds.Value;
        }

        // Aspect ratio (Kling, Luma, etc.)
        if (!string.IsNullOrWhiteSpace(options.AspectRatio))
        {
            inputPayload["aspect_ratio"] = options.AspectRatio;
        }
        else if (options.Width > 0 && options.Height > 0)
        {
            // Compute aspect ratio string from dimensions
            var gcd = Gcd(options.Width, options.Height);
            inputPayload["aspect_ratio"] = $"{options.Width / gcd}:{options.Height / gcd}";
        }

        if (!string.IsNullOrWhiteSpace(options.NegativePrompt))
            inputPayload["negative_prompt"] = options.NegativePrompt;

        if (!string.IsNullOrWhiteSpace(options.Seed) && int.TryParse(options.Seed, out var seed))
            inputPayload["seed"] = seed;

        var json = JsonSerializer.Serialize(inputPayload);

        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, submitEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        submitRequest.Headers.Authorization = new AuthenticationHeaderValue("Key", options.ApiKey);

        _logger.LogInformation("[Fal.ai Video] Submitting to queue: {Endpoint}", submitEndpoint);

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Fal.ai Video] Submit error {StatusCode}: {Body}", (int)submitResponse.StatusCode, submitBody);
            throw new InvalidOperationException(
                $"Falha ao submeter geração de vídeo na fal.ai. Status: {(int)submitResponse.StatusCode}. Detalhes: {submitBody}");
        }

        using var submitDoc = JsonDocument.Parse(submitBody);

        // Check for synchronous response
        if (!submitDoc.RootElement.TryGetProperty("request_id", out var requestIdProp))
        {
            _logger.LogInformation("[Fal.ai Video] Synchronous response received, parsing directly");
            return ParseResponse(submitBody);
        }

        var requestId = requestIdProp.GetString();
        if (string.IsNullOrEmpty(requestId))
            throw new InvalidOperationException("Fal.ai queue response did not contain a valid request_id.");

        var statusUrl = submitDoc.RootElement.TryGetProperty("status_url", out var statusUrlProp)
            ? statusUrlProp.GetString() ?? $"https://queue.fal.run/{modelId}/requests/{requestId}/status"
            : $"https://queue.fal.run/{modelId}/requests/{requestId}/status";

        var resultUrl = submitDoc.RootElement.TryGetProperty("response_url", out var resultUrlProp)
            ? resultUrlProp.GetString() ?? $"https://queue.fal.run/{modelId}/requests/{requestId}"
            : $"https://queue.fal.run/{modelId}/requests/{requestId}";

        _logger.LogInformation("[Fal.ai Video] Request queued. ID: {RequestId}. Polling...", requestId);

        return await PollForResultAsync(requestId, options.ApiKey, statusUrl, resultUrl, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string requestId,
        string apiKey,
        string statusUrl,
        string resultUrl,
        CancellationToken ct)
    {
        const int maxAttempts = 120; // 120 × 3s = 360s max (video takes longer)
        const int delayMs = 3000;

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            using var statusReq = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);

            using var statusRes = await _httpClient.SendAsync(statusReq, ct);
            var statusBody = await statusRes.Content.ReadAsStringAsync(ct);

            if (!statusRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Fal.ai Video] Status check error {StatusCode}: {Body}", (int)statusRes.StatusCode, statusBody);
                throw new InvalidOperationException(
                    $"Falha ao verificar status na fal.ai. Status: {(int)statusRes.StatusCode}");
            }

            using var statusDoc = JsonDocument.Parse(statusBody);
            var status = statusDoc.RootElement.TryGetProperty("status", out var s)
                ? s.GetString()
                : "UNKNOWN";

            _logger.LogDebug("[Fal.ai Video] Poll {Attempt}/{Max}: status = {Status}", i + 1, maxAttempts, status);

            if (status == "COMPLETED")
            {
                _logger.LogInformation("[Fal.ai Video] Generation completed for request {RequestId}", requestId);

                using var resultReq = new HttpRequestMessage(HttpMethod.Get, resultUrl);
                resultReq.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);

                using var resultRes = await _httpClient.SendAsync(resultReq, ct);
                var resultBody = await resultRes.Content.ReadAsStringAsync(ct);

                if (!resultRes.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[Fal.ai Video] Result fetch error {StatusCode}: {Body}", (int)resultRes.StatusCode, resultBody);
                    throw new InvalidOperationException(
                        $"Falha ao obter resultado de vídeo da fal.ai. Status: {(int)resultRes.StatusCode}");
                }

                return ParseResponse(resultBody);
            }
            else if (status == "FAILED")
            {
                var errorDetail = statusDoc.RootElement.TryGetProperty("error", out var errProp)
                    ? errProp.GetRawText()
                    : "(sem detalhes)";
                throw new InvalidOperationException(
                    $"Fal.ai reportou falha na geração de vídeo para o request {requestId}. Erro: {errorDetail}");
            }

            await Task.Delay(delayMs, ct);
        }

        throw new TimeoutException(
            $"Tempo limite excedido aguardando geração de vídeo na fal.ai (Request ID: {requestId}). " +
            "Tente novamente ou use um modelo mais rápido.");
    }

    private static VideoGenerationResult ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        string? videoUrl = null;
        string? thumbnailUrl = null;

        // Most fal.ai video models: { "video": { "url": "..." } }
        if (root.TryGetProperty("video", out var videoProp))
        {
            if (videoProp.ValueKind == JsonValueKind.Object)
                videoUrl = videoProp.TryGetProperty("url", out var u) ? u.GetString() : null;
            else if (videoProp.ValueKind == JsonValueKind.String)
                videoUrl = videoProp.GetString();
        }

        // Some models: { "videos": [{ "url": "..." }] }
        if (videoUrl == null && root.TryGetProperty("videos", out var videos) &&
            videos.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in videos.EnumerateArray())
            {
                videoUrl = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (!string.IsNullOrEmpty(videoUrl)) break;
            }
        }

        // Thumbnail
        if (root.TryGetProperty("thumbnail", out var thumb))
            thumbnailUrl = thumb.ValueKind == JsonValueKind.String ? thumb.GetString() :
                           thumb.TryGetProperty("url", out var tu) ? tu.GetString() : null;

        if (string.IsNullOrEmpty(videoUrl))
            throw new InvalidOperationException($"Fal.ai retornou resposta sem URL de vídeo: {responseBody[..Math.Min(500, responseBody.Length)]}");

        return new VideoGenerationResult(videoUrl, thumbnailUrl, null);
    }

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
