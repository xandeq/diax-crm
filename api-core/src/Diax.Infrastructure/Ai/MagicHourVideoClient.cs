using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for Magic Hour (https://magichour.ai).
/// Free tier com créditos DIÁRIOS renováveis (único do mercado, pesquisa 2026-07).
/// Fluxo: POST /v1/text-to-video (Bearer mhk_...) → { id, credits_charged } →
/// poll GET /v1/video-projects/{id} até status complete/error/canceled.
/// A URL de download expira (expires_at) — o storage durável do job resolve.
/// </summary>
public class MagicHourVideoClient : IAiVideoGenerationClient
{
    private const string DefaultBaseUrl = "https://api.magichour.ai/v1";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MagicHourVideoClient> _logger;

    public string ProviderName => "magichour";

    // Text-to-video por enquanto (image-to-video deles usa upload de asset próprio).
    public bool SupportsImageToVideo => false;

    public MagicHourVideoClient(HttpClient httpClient, ILogger<MagicHourVideoClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _logger = logger;
    }

    public async Task<VideoGenerationResult> GenerateAsync(
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for Magic Hour.");

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("Magic Hour exige um prompt de texto para text-to-video.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? DefaultBaseUrl : options.BaseUrl.TrimEnd('/');

        var payload = new Dictionary<string, object?>
        {
            ["name"] = "DIAX video",
            ["end_seconds"] = Math.Max(options.DurationSeconds ?? 5, 5),
            ["style"] = new Dictionary<string, object?> { ["prompt"] = prompt },
            ["aspect_ratio"] = NormalizeAspectRatio(options),
        };
        // options.Model carrega o modelo do Magic Hour (ex.: "kling-3.0"); o valor
        // "default" deixa o Magic Hour escolher o modelo mais barato do plano.
        if (!string.IsNullOrWhiteSpace(options.Model) &&
            !options.Model.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            payload["model"] = options.Model;
        }

        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/text-to-video")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        submitRequest.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        _logger.LogInformation("[MagicHour] Submitting text-to-video (model: {Model})", options.Model);

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[MagicHour] Submit failed HTTP {Status}: {Body}",
                (int)submitResponse.StatusCode, Truncate(submitBody, 300));
            throw new InvalidOperationException(
                $"Falha ao submeter vídeo no Magic Hour. HTTP {(int)submitResponse.StatusCode}: {ExtractErrorMessage(submitBody)}");
        }

        string? projectId = null;
        int? creditsCharged = null;
        try
        {
            using var doc = JsonDocument.Parse(submitBody);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
                projectId = idProp.GetString();
            if (doc.RootElement.TryGetProperty("credits_charged", out var cc) && cc.ValueKind == JsonValueKind.Number)
                creditsCharged = cc.GetInt32();
        }
        catch (JsonException) { }

        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException(
                $"Resposta do Magic Hour sem project id: {Truncate(submitBody, 300)}");

        _logger.LogInformation(
            "[MagicHour] Project {ProjectId} submitted ({Credits} créditos). Polling...",
            projectId, creditsCharged ?? 0);

        return await PollForResultAsync(baseUrl, projectId, options.ApiKey, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string baseUrl, string projectId, string apiKey, CancellationToken ct)
    {
        const int maxAttempts = 120; // 120 × 5s = 10 minutos de teto
        var pollDelay = TimeSpan.FromSeconds(5);
        var detailsUrl = $"{baseUrl}/video-projects/{projectId}";

        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(pollDelay, ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, detailsUrl);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                    throw new InvalidOperationException(
                        $"Polling do Magic Hour falhou. HTTP {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
                continue; // 5xx/429 transitório
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;

            _logger.LogDebug("[MagicHour] Poll {Attempt}/{Max}: status={Status}", i + 1, maxAttempts, status);

            switch (status)
            {
                case "complete":
                {
                    if (root.TryGetProperty("downloads", out var downloads)
                        && downloads.ValueKind == JsonValueKind.Array
                        && downloads.GetArrayLength() > 0
                        && downloads[0].TryGetProperty("url", out var urlProp))
                    {
                        var videoUrl = urlProp.GetString();
                        if (!string.IsNullOrWhiteSpace(videoUrl))
                        {
                            _logger.LogInformation("[MagicHour] Video complete. Project: {ProjectId}", projectId);
                            return new VideoGenerationResult(videoUrl, null, null);
                        }
                    }
                    throw new InvalidOperationException("Magic Hour retornou 'complete' sem URL de download.");
                }
                case "error":
                case "canceled":
                {
                    string? message = null;
                    if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object
                        && err.TryGetProperty("message", out var msg))
                        message = msg.GetString();
                    throw new InvalidOperationException(
                        $"Geração de vídeo falhou no Magic Hour ({status}): {message ?? "sem detalhes"}");
                }
                // draft | queued | rendering → continua
            }
        }

        throw new TimeoutException(
            "Timeout aguardando resultado de vídeo do Magic Hour (10 min). Tente novamente.");
    }

    private static string NormalizeAspectRatio(VideoGenerationOptions options)
    {
        var ar = options.AspectRatio?.Trim();
        if (ar is "16:9" or "9:16" or "1:1") return ar;
        if (options.Width > 0 && options.Height > 0)
        {
            if (options.Width > options.Height) return "16:9";
            if (options.Width < options.Height) return "9:16";
            return "1:1";
        }
        return "16:9";
    }

    private static string ExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? Truncate(body, 200);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.Object && err.TryGetProperty("message", out var m2))
                    return m2.GetString() ?? Truncate(body, 200);
                return err.GetString() ?? Truncate(body, 200);
            }
        }
        catch (JsonException) { }
        return Truncate(body, 200);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
