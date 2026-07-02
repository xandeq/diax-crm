using System.Net;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for WaveSpeed AI (https://wavespeed.ai).
/// Um dos providers mais baratos do mercado: Wan 2.2 Ultra Fast a $0.01/s ($0.05 por vídeo de 5s).
/// Fluxo: POST /api/v3/{model_path} (Bearer) → { data: { id } } → poll GET /api/v3/predictions/{id}/result
/// até status completed/failed. Statuses: created | processing | completed | failed.
/// </summary>
public class WaveSpeedVideoClient : IAiVideoGenerationClient
{
    private const string DefaultBaseUrl = "https://api.wavespeed.ai/api/v3";

    private readonly HttpClient _httpClient;
    private readonly ILogger<WaveSpeedVideoClient> _logger;

    public string ProviderName => "wavespeed";

    // Só text-to-video por enquanto — os paths i2v da WaveSpeed têm payload próprio (campo image).
    public bool SupportsImageToVideo => false;

    public WaveSpeedVideoClient(HttpClient httpClient, ILogger<WaveSpeedVideoClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // por request; a espera longa é no loop de polling
        _logger = logger;
    }

    public async Task<VideoGenerationResult> GenerateAsync(
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for WaveSpeed.");

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("WaveSpeed exige um prompt de texto para text-to-video.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? DefaultBaseUrl : options.BaseUrl.TrimEnd('/');
        // options.Model é o path do modelo, ex: "wavespeed-ai/wan-2.2/t2v-480p-ultra-fast"
        var submitUrl = $"{baseUrl}/{options.Model.TrimStart('/')}";

        var payload = new Dictionary<string, object?>
        {
            ["prompt"] = prompt,
            ["duration"] = Math.Max(options.DurationSeconds ?? 5, 5), // mínimo cobrado é 5s
        };
        if (!string.IsNullOrWhiteSpace(options.NegativePrompt))
            payload["negative_prompt"] = options.NegativePrompt;
        if (!string.IsNullOrWhiteSpace(options.Seed) && long.TryParse(options.Seed, out var seed))
            payload["seed"] = seed;

        // Wan 2.2 480p aceita 832*480 (paisagem) ou 480*832 (retrato)
        payload["size"] = options.Width >= options.Height ? "832*480" : "480*832";

        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, submitUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        submitRequest.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        _logger.LogInformation("[WaveSpeed] Submitting video task. Model: {Model}", options.Model);

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[WaveSpeed] Submit failed HTTP {Status}: {Body}",
                (int)submitResponse.StatusCode, Truncate(submitBody, 300));
            throw new InvalidOperationException(
                $"Falha ao submeter vídeo no WaveSpeed. HTTP {(int)submitResponse.StatusCode}: {ExtractErrorMessage(submitBody)}");
        }

        var taskId = ExtractTaskId(submitBody)
            ?? throw new InvalidOperationException(
                $"Resposta do WaveSpeed sem task id: {Truncate(submitBody, 300)}");

        _logger.LogInformation("[WaveSpeed] Task submitted. ID: {TaskId}. Polling...", taskId);

        return await PollForResultAsync(baseUrl, taskId, options.ApiKey, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string baseUrl, string taskId, string apiKey, CancellationToken ct)
    {
        const int maxAttempts = 120;          // 120 × 5s = 10 minutos de teto
        var pollDelay = TimeSpan.FromSeconds(5);
        var resultUrl = $"{baseUrl}/predictions/{taskId}/result";

        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(pollDelay, ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, resultUrl);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                // 5xx/429 transitório no polling: continua tentando
                _logger.LogDebug("[WaveSpeed] Poll HTTP {Status} (attempt {Attempt}/{Max})",
                    (int)response.StatusCode, i + 1, maxAttempts);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                    throw new InvalidOperationException(
                        $"Polling do WaveSpeed falhou. HTTP {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
                continue;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                continue;

            var status = data.TryGetProperty("status", out var s) ? s.GetString() : null;
            _logger.LogDebug("[WaveSpeed] Poll {Attempt}/{Max}: status={Status}", i + 1, maxAttempts, status);

            switch (status)
            {
                case "completed":
                {
                    if (data.TryGetProperty("outputs", out var outputs)
                        && outputs.ValueKind == JsonValueKind.Array
                        && outputs.GetArrayLength() > 0)
                    {
                        var videoUrl = outputs[0].GetString();
                        if (!string.IsNullOrWhiteSpace(videoUrl))
                        {
                            _logger.LogInformation("[WaveSpeed] Video completed. Task: {TaskId}", taskId);
                            return new VideoGenerationResult(videoUrl, null, null);
                        }
                    }
                    throw new InvalidOperationException(
                        "WaveSpeed retornou 'completed' sem URLs de saída.");
                }
                case "failed":
                {
                    var error = data.TryGetProperty("error", out var e) ? e.GetString() : null;
                    throw new InvalidOperationException(
                        $"Geração de vídeo falhou no WaveSpeed: {error ?? "sem detalhes"}");
                }
                // created | processing → continua
            }
        }

        throw new TimeoutException(
            "Timeout aguardando resultado de vídeo do WaveSpeed (10 min). Tente novamente.");
    }

    private static string? ExtractTaskId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data)
                && data.TryGetProperty("id", out var id))
                return id.GetString();
        }
        catch (JsonException) { /* resposta não-JSON */ }
        return null;
    }

    private static string ExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? Truncate(body, 200);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString() ?? Truncate(body, 200);
        }
        catch (JsonException) { }
        return Truncate(body, 200);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
