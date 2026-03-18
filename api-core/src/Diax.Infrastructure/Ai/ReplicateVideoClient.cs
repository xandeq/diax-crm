using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for Replicate API.
/// Supports 200+ models including SVD, AnimateDiff, Make-A-Video, etc.
/// Uses async polling model: POST prediction → poll status → GET result.
/// Auth: Authorization: Bearer {api_token}
/// Docs: https://replicate.com/docs/api/rest
/// </summary>
public class ReplicateVideoClient : IAiVideoGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReplicateVideoClient> _logger;

    public string ProviderName => "replicate";
    public bool SupportsImageToVideo => true;

    public ReplicateVideoClient(HttpClient httpClient, ILogger<ReplicateVideoClient> logger)
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
            throw new InvalidOperationException("API token not configured for Replicate.");

        var baseUrl = options.BaseUrl ?? "https://api.replicate.com/v1";
        var submitEndpoint = $"{baseUrl}/predictions";

        // Build input based on model
        var input = BuildInput(options, prompt, referenceImageBase64);

        var payload = new Dictionary<string, object>
        {
            ["version"] = options.Model,
            ["input"] = input,
            ["webhook"] = "https://example.com/webhook" // Replicate requires webhook, ignored for sync wait
        };

        var json = JsonSerializer.Serialize(payload);

        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, submitEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        submitRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        _logger.LogInformation("[Replicate] Submitting prediction: model={Model}", options.Model);

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Replicate] Submit error {StatusCode}: {Body}",
                (int)submitResponse.StatusCode, submitBody[..Math.Min(500, submitBody.Length)]);

            if (submitResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    "API token inválido no Replicate. Verifique as credenciais.");
            }

            throw new InvalidOperationException(
                $"Erro ao submeter predição no Replicate. Status: {(int)submitResponse.StatusCode}");
        }

        using var submitDoc = JsonDocument.Parse(submitBody);
        var predictionUrl = submitDoc.RootElement.TryGetProperty("urls", out var urls) &&
                           urls.TryGetProperty("get", out var getUrl)
            ? getUrl.GetString()
            : null;

        if (string.IsNullOrEmpty(predictionUrl))
            throw new InvalidOperationException("Replicate não retornou URL de predição.");

        _logger.LogInformation("[Replicate] Prediction submitted. Polling: {Url}", predictionUrl);

        return await PollForResultAsync(predictionUrl, options.ApiKey, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string predictionUrl,
        string apiToken,
        CancellationToken ct)
    {
        const int maxAttempts = 300; // 5 minutes at 1s intervals (videos take time)
        const int delayMs = 1000;

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            using var statusReq = new HttpRequestMessage(HttpMethod.Get, predictionUrl);
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            using var statusRes = await _httpClient.SendAsync(statusReq, ct);
            var statusBody = await statusRes.Content.ReadAsStringAsync(ct);

            if (!statusRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Replicate] Status check error {StatusCode}", (int)statusRes.StatusCode);
                throw new InvalidOperationException(
                    $"Erro ao verificar status da predição. Status: {(int)statusRes.StatusCode}");
            }

            using var statusDoc = JsonDocument.Parse(statusBody);
            var status = statusDoc.RootElement.TryGetProperty("status", out var s)
                ? s.GetString()
                : "unknown";

            _logger.LogDebug("[Replicate] Poll {Attempt}/{Max}: status={Status}", i + 1, maxAttempts, status);

            if (status == "succeeded")
            {
                return ParseResponse(statusBody);
            }
            else if (status == "failed" || status == "canceled")
            {
                var error = statusDoc.RootElement.TryGetProperty("error", out var errProp)
                    ? errProp.GetString()
                    : "sem detalhes";
                throw new InvalidOperationException(
                    $"Predição falhou no Replicate. Status: {status}. Erro: {error}");
            }

            await Task.Delay(delayMs, ct);
        }

        throw new TimeoutException(
            "Timeout aguardando resultado de vídeo do Replicate. " +
            "Modelos podem levar vários minutos — tente novamente.");
    }

    private Dictionary<string, object> BuildInput(
        VideoGenerationOptions options,
        string? prompt,
        string? referenceImageBase64)
    {
        var input = new Dictionary<string, object>
        {
            ["prompt"] = prompt ?? "A professional video"
        };

        // Image-to-video (some models accept image input)
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            input["image"] = $"data:image/png;base64,{referenceImageBase64}";
            _logger.LogInformation("[Replicate] img2video mode");
        }

        // Duration
        if (options.DurationSeconds.HasValue)
        {
            input["num_frames"] = options.DurationSeconds.Value * 8; // Default ~8 fps
        }

        // Negative prompt
        if (!string.IsNullOrWhiteSpace(options.NegativePrompt))
            input["negative_prompt"] = options.NegativePrompt;

        // Seed for reproducibility
        if (!string.IsNullOrWhiteSpace(options.Seed) && int.TryParse(options.Seed, out var seed))
            input["seed"] = seed;

        return input;
    }

    private static VideoGenerationResult ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        string? videoUrl = null;
        string? thumbnailUrl = null;

        // Replicate returns output as an array or object
        if (root.TryGetProperty("output", out var output))
        {
            if (output.ValueKind == JsonValueKind.Array)
            {
                // Array of URLs: pick first video
                foreach (var item in output.EnumerateArray())
                {
                    videoUrl = item.ValueKind == JsonValueKind.String
                        ? item.GetString()
                        : null;
                    if (!string.IsNullOrEmpty(videoUrl))
                        break;
                }
            }
            else if (output.ValueKind == JsonValueKind.String)
            {
                videoUrl = output.GetString();
            }
            else if (output.ValueKind == JsonValueKind.Object)
            {
                // Some models return { "video": "..." }
                videoUrl = output.TryGetProperty("video", out var v)
                    ? v.GetString()
                    : null;
            }
        }

        if (string.IsNullOrEmpty(videoUrl))
            throw new InvalidOperationException(
                $"Replicate retornou resposta sem URL de vídeo: {responseBody[..Math.Min(300, responseBody.Length)]}");

        return new VideoGenerationResult(videoUrl, thumbnailUrl, null);
    }
}
