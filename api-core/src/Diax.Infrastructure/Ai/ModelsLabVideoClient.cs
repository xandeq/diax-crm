using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for ModelsLab (https://modelslab.com).
/// Preços muito baixos (WAN $0.03–0.08/vídeo, Seedance from $0.06/clip).
/// Fluxo: POST /api/v6/video/text2video (key vai NO BODY — padrão da ModelsLab) →
/// status "success" (output direto) ou "processing" (id) → poll POST /api/v6/video/fetch/{id}.
/// Atenção: a API da ModelsLab às vezes retorna o campo de erro como "messege" (typo deles).
/// </summary>
public class ModelsLabVideoClient : IAiVideoGenerationClient
{
    private const string DefaultBaseUrl = "https://modelslab.com/api/v6";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ModelsLabVideoClient> _logger;

    public string ProviderName => "modelslab";

    // Só text-to-video por enquanto — o endpoint img2video tem payload próprio não integrado.
    public bool SupportsImageToVideo => false;

    public ModelsLabVideoClient(HttpClient httpClient, ILogger<ModelsLabVideoClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // o submit pode segurar até responder processing
        _logger = logger;
    }

    public async Task<VideoGenerationResult> GenerateAsync(
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for ModelsLab.");

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("ModelsLab exige um prompt de texto para text-to-video.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? DefaultBaseUrl : options.BaseUrl.TrimEnd('/');
        var submitUrl = $"{baseUrl}/video/text2video";

        // ~5 frames/s no WAN — num_frames a partir da duração pedida (teto conservador de 81)
        var numFrames = Math.Clamp((options.DurationSeconds ?? 5) * 5, 15, 81);

        var payload = new Dictionary<string, object?>
        {
            ["key"] = options.ApiKey,
            ["model_id"] = options.Model,
            ["prompt"] = prompt,
            ["negative_prompt"] = options.NegativePrompt,
            ["width"] = Math.Min(options.Width, 1024),
            ["height"] = Math.Min(options.Height, 1024),
            ["num_frames"] = numFrames,
            ["output_type"] = "mp4",
        };
        if (!string.IsNullOrWhiteSpace(options.Seed) && long.TryParse(options.Seed, out var seed))
            payload["seed"] = seed;

        _logger.LogInformation("[ModelsLab] Submitting video task. Model: {Model}", options.Model);

        using var response = await _httpClient.PostAsync(
            submitUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[ModelsLab] Submit failed HTTP {Status}: {Body}",
                (int)response.StatusCode, Truncate(body, 300));
            throw new InvalidOperationException(
                $"Falha ao submeter vídeo no ModelsLab. HTTP {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
        }

        var parsed = ParseSubmitResponse(body);
        if (parsed.VideoUrl != null)
        {
            _logger.LogInformation("[ModelsLab] Video ready immediately.");
            return new VideoGenerationResult(parsed.VideoUrl, null, null);
        }

        if (parsed.RequestId == null)
            throw new InvalidOperationException(
                $"Resposta do ModelsLab sem output nem request id: {Truncate(body, 300)}");

        _logger.LogInformation("[ModelsLab] Task queued. ID: {RequestId}. ETA: {Eta}s. Polling...",
            parsed.RequestId, parsed.EtaSeconds ?? 0);

        return await PollForResultAsync(baseUrl, parsed.RequestId.Value, options.ApiKey, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string baseUrl, long requestId, string apiKey, CancellationToken ct)
    {
        const int maxAttempts = 60;           // 60 × 10s = 10 minutos de teto
        var pollDelay = TimeSpan.FromSeconds(10);
        var fetchUrl = $"{baseUrl}/video/fetch/{requestId}";
        var fetchPayload = JsonSerializer.Serialize(new { key = apiKey });

        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(pollDelay, ct);

            using var response = await _httpClient.PostAsync(
                fetchUrl,
                new StringContent(fetchPayload, Encoding.UTF8, "application/json"),
                ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("[ModelsLab] Poll HTTP {Status} (attempt {Attempt}/{Max})",
                    (int)response.StatusCode, i + 1, maxAttempts);
                continue; // transitório — o fetch da ModelsLab oscila
            }

            var parsed = ParseSubmitResponse(body);
            _logger.LogDebug("[ModelsLab] Poll {Attempt}/{Max}: status={Status}", i + 1, maxAttempts, parsed.Status);

            if (parsed.VideoUrl != null)
            {
                _logger.LogInformation("[ModelsLab] Video completed. Request: {RequestId}", requestId);
                return new VideoGenerationResult(parsed.VideoUrl, null, null);
            }

            if (parsed.Status is "error" or "failed")
                throw new InvalidOperationException(
                    $"Geração de vídeo falhou no ModelsLab: {parsed.ErrorMessage ?? "sem detalhes"}");

            // processing → continua
        }

        throw new TimeoutException(
            "Timeout aguardando resultado de vídeo do ModelsLab (10 min). Tente novamente.");
    }

    private sealed record SubmitResponse(
        string? Status, long? RequestId, string? VideoUrl, double? EtaSeconds, string? ErrorMessage);

    private static SubmitResponse ParseSubmitResponse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;

            long? id = null;
            if (root.TryGetProperty("id", out var idProp))
            {
                if (idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt64(out var idNum))
                    id = idNum;
                else if (idProp.ValueKind == JsonValueKind.String && long.TryParse(idProp.GetString(), out var idStr))
                    id = idStr;
            }

            string? videoUrl = null;
            if (root.TryGetProperty("output", out var output)
                && output.ValueKind == JsonValueKind.Array
                && output.GetArrayLength() > 0)
            {
                videoUrl = output[0].GetString();
                if (string.IsNullOrWhiteSpace(videoUrl)) videoUrl = null;
            }

            double? eta = null;
            if (root.TryGetProperty("eta", out var etaProp) && etaProp.ValueKind == JsonValueKind.Number)
                eta = etaProp.GetDouble();

            // ModelsLab usa "message" OU "messege" (typo histórico da API deles)
            string? error = null;
            if (root.TryGetProperty("message", out var msg)) error = msg.GetString();
            else if (root.TryGetProperty("messege", out var msg2)) error = msg2.GetString();

            return new SubmitResponse(status, id, videoUrl, eta, error);
        }
        catch (JsonException)
        {
            return new SubmitResponse(null, null, null, null, Truncate(body, 200));
        }
    }

    private static string ExtractErrorMessage(string body)
    {
        var parsed = ParseSubmitResponse(body);
        return parsed.ErrorMessage ?? Truncate(body, 200);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
