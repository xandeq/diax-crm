using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for Shotstack API.
/// Shotstack specializes in video composition/templating, not AI generation.
/// This implementation treats Shotstack as a video composition service.
/// Uses async polling model: POST render → poll status → GET result.
/// Auth: Authorization: Bearer {api_key}
/// Docs: https://shotstack.io/docs/api-reference/render
/// </summary>
public class ShotstackVideoClient : IAiVideoGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShotstackVideoClient> _logger;

    public string ProviderName => "shotstack";
    public bool SupportsImageToVideo => false; // Shotstack is composition, not generation

    public ShotstackVideoClient(HttpClient httpClient, ILogger<ShotstackVideoClient> logger)
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
            throw new InvalidOperationException("API key not configured for Shotstack.");

        var baseUrl = options.BaseUrl ?? "https://api.shotstack.io/v1";
        var renderEndpoint = $"{baseUrl}/render";

        // Build a simple composition request (text-only for free tier)
        var payload = BuildComposition(prompt, options);
        var json = JsonSerializer.Serialize(payload);

        using var renderRequest = new HttpRequestMessage(HttpMethod.Post, renderEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        renderRequest.Headers.TryAddWithoutValidation("x-api-key", options.ApiKey);

        _logger.LogInformation("[Shotstack] Submitting render: duration={Duration}s",
            options.DurationSeconds ?? 5);

        using var renderResponse = await _httpClient.SendAsync(renderRequest, ct);
        var renderBody = await renderResponse.Content.ReadAsStringAsync(ct);

        if (!renderResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Shotstack] Submit error {StatusCode}: {Body}",
                (int)renderResponse.StatusCode, renderBody[..Math.Min(500, renderBody.Length)]);

            if (renderResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    "API key inválida no Shotstack. Verifique as credenciais.");
            }

            throw new InvalidOperationException(
                $"Erro ao submeter render no Shotstack. Status: {(int)renderResponse.StatusCode}");
        }

        using var renderDoc = JsonDocument.Parse(renderBody);
        var renderId = renderDoc.RootElement.TryGetProperty("response", out var resp) &&
                       resp.TryGetProperty("id", out var id)
            ? id.GetString()
            : null;

        if (string.IsNullOrEmpty(renderId))
            throw new InvalidOperationException("Shotstack não retornou ID de render.");

        var statusUrl = $"{baseUrl}/render/{renderId}";

        _logger.LogInformation("[Shotstack] Render submitted. ID: {RenderId}. Polling...", renderId);

        return await PollForResultAsync(statusUrl, options.ApiKey, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string statusUrl,
        string apiKey,
        CancellationToken ct)
    {
        const int maxAttempts = 120; // 10 minutes at 5s intervals
        const int delayMs = 5000;

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            using var statusReq = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            statusReq.Headers.TryAddWithoutValidation("x-api-key", apiKey);

            using var statusRes = await _httpClient.SendAsync(statusReq, ct);
            var statusBody = await statusRes.Content.ReadAsStringAsync(ct);

            if (!statusRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Shotstack] Status check error {StatusCode}", (int)statusRes.StatusCode);
                throw new InvalidOperationException(
                    $"Erro ao verificar status do render. Status: {(int)statusRes.StatusCode}");
            }

            using var statusDoc = JsonDocument.Parse(statusBody);
            var response = statusDoc.RootElement.TryGetProperty("response", out var resp)
                ? resp
                : statusDoc.RootElement;

            var status = response.TryGetProperty("status", out var s)
                ? s.GetString()
                : "unknown";

            _logger.LogDebug("[Shotstack] Poll {Attempt}/{Max}: status={Status}", i + 1, maxAttempts, status);

            if (status == "done")
            {
                return ParseResponse(statusBody);
            }
            else if (status == "failed")
            {
                var error = response.TryGetProperty("error", out var errProp)
                    ? errProp.GetString()
                    : "sem detalhes";
                throw new InvalidOperationException(
                    $"Render falhou no Shotstack. Erro: {error}");
            }

            await Task.Delay(delayMs, ct);
        }

        throw new TimeoutException(
            "Timeout aguardando render do Shotstack. Tente novamente.");
    }

    private Dictionary<string, object> BuildComposition(string? prompt, VideoGenerationOptions options)
    {
        // Simple Shotstack composition: text track with background
        var output = new Dictionary<string, object>
        {
            ["format"] = "mp4",
            ["resolution"] = "hd"
        };

        var tracks = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["clips"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["asset"] = new Dictionary<string, object>
                        {
                            ["type"] = "text",
                            ["text"] = prompt ?? "Shotstack Render"
                        },
                        ["start"] = 0,
                        ["length"] = options.DurationSeconds ?? 5
                    }
                }
            }
        };

        // Shotstack API root level: { timeline: {...}, output: {...} } — no "edit" wrapper
        return new Dictionary<string, object>
        {
            ["timeline"] = new Dictionary<string, object>
            {
                ["tracks"] = tracks
            },
            ["output"] = output
        };
    }

    private static VideoGenerationResult ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var response = doc.RootElement.TryGetProperty("response", out var resp)
            ? resp
            : doc.RootElement;

        string? videoUrl = null;

        // Shotstack response: { "render": "..." } or { "url": "..." }
        if (response.TryGetProperty("render", out var render))
        {
            videoUrl = render.GetString();
        }
        else if (response.TryGetProperty("url", out var url))
        {
            videoUrl = url.GetString();
        }

        if (string.IsNullOrEmpty(videoUrl))
            throw new InvalidOperationException(
                $"Shotstack retornou resposta sem URL de vídeo: {responseBody[..Math.Min(300, responseBody.Length)]}");

        return new VideoGenerationResult(videoUrl, null, null);
    }
}
