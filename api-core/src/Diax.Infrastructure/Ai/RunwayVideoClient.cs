using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for Runway ML official API.
/// Supports Gen-3 and Gen-4 models with text-to-video and image-to-video capabilities.
/// Uses async polling model: POST task → poll /v1/tasks/{id} → GET result.
/// Auth: Authorization: Bearer {api_key} + X-Runway-Version: 2024-11-06
/// Docs: https://docs.dev.runwayml.com/api
/// </summary>
public class RunwayVideoClient : IAiVideoGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RunwayVideoClient> _logger;

    public string ProviderName => "runway";
    public bool SupportsImageToVideo => true;

    public RunwayVideoClient(HttpClient httpClient, ILogger<RunwayVideoClient> logger)
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
            throw new InvalidOperationException("API key not configured for Runway.");

        var baseUrl = options.BaseUrl ?? "https://api.dev.runwayml.com/v1";
        var endpoint = $"{baseUrl}/image_to_video";

        // Build request payload per Runway API spec
        var payload = new Dictionary<string, object?>
        {
            ["promptText"] = prompt ?? "A professional cinematic video",
            ["model"] = options.Model ?? "gen3a_turbo",
            // promptImage is required by Runway — null triggers text-to-video mode on supported models
            ["promptImage"] = string.IsNullOrWhiteSpace(referenceImageBase64)
                ? null
                : (object)$"data:image/png;base64,{referenceImageBase64}",
        };

        // Duration (Runway supports 5 or 10 seconds)
        if (options.DurationSeconds.HasValue)
            payload["duration"] = options.DurationSeconds.Value <= 5 ? 5 : 10;

        // Aspect ratio
        if (!string.IsNullOrWhiteSpace(options.AspectRatio))
            payload["ratio"] = options.AspectRatio;

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.TryAddWithoutValidation("X-Runway-Version", "2024-11-06");

        _logger.LogInformation("[Runway] Submitting task: model={Model}", options.Model ?? "gen3a_turbo");

        HttpResponseMessage submitResponse;
        try
        {
            submitResponse = await _httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("Timeout ao submeter tarefa no Runway. Tente novamente.");
        }

        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Runway] Submit error {StatusCode}: {Body}",
                (int)submitResponse.StatusCode, submitBody[..Math.Min(500, submitBody.Length)]);

            if (submitResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                submitResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    "API key inválida ou expirada no Runway. Verifique as credenciais.");
            }

            if (submitResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException(
                    "Limite de requisições atingido no Runway. Tente novamente mais tarde.");
            }

            throw new InvalidOperationException(
                $"Erro ao submeter tarefa no Runway. Status: {(int)submitResponse.StatusCode}. " +
                $"Detalhes: {submitBody[..Math.Min(500, submitBody.Length)]}");
        }

        using var submitDoc = JsonDocument.Parse(submitBody);
        var taskId = submitDoc.RootElement.TryGetProperty("id", out var idProp)
            ? idProp.GetString()
            : null;

        if (string.IsNullOrEmpty(taskId))
            throw new InvalidOperationException("Runway não retornou ID de tarefa.");

        _logger.LogInformation("[Runway] Task submitted. ID: {TaskId}. Polling...", taskId);

        return await PollForResultAsync(baseUrl, taskId, options.ApiKey, ct);
    }

    private async Task<VideoGenerationResult> PollForResultAsync(
        string baseUrl,
        string taskId,
        string apiKey,
        CancellationToken ct)
    {
        const int maxAttempts = 150; // 5 minutes at 2s intervals
        const int delayMs = 2000;
        var statusUrl = $"{baseUrl}/tasks/{taskId}";

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(delayMs, ct);

            using var statusReq = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            statusReq.Headers.TryAddWithoutValidation("X-Runway-Version", "2024-11-06");

            using var statusRes = await _httpClient.SendAsync(statusReq, ct);
            var statusBody = await statusRes.Content.ReadAsStringAsync(ct);

            if (!statusRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Runway] Status check error {StatusCode}", (int)statusRes.StatusCode);
                throw new InvalidOperationException(
                    $"Erro ao verificar status da tarefa Runway. Status: {(int)statusRes.StatusCode}");
            }

            using var doc = JsonDocument.Parse(statusBody);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : "UNKNOWN";

            _logger.LogDebug("[Runway] Poll {Attempt}/{Max}: status={Status}", i + 1, maxAttempts, status);

            if (status == "SUCCEEDED")
            {
                return ParseResponse(statusBody);
            }
            else if (status is "FAILED" or "CANCELLED" or "CANCELED")
            {
                var failure = doc.RootElement.TryGetProperty("failure", out var f) ? f.GetString() : "sem detalhes";
                throw new InvalidOperationException(
                    $"Tarefa falhou no Runway. Status: {status}. Erro: {failure}");
            }
            // PENDING, RUNNING — continue polling
        }

        throw new TimeoutException(
            "Timeout aguardando resultado de vídeo do Runway. Tente novamente em instantes.");
    }

    private static VideoGenerationResult ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        string? videoUrl = null;

        // Runway response: { "output": ["url1", ...] }
        if (root.TryGetProperty("output", out var output))
        {
            if (output.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in output.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        videoUrl = item.GetString();
                        if (!string.IsNullOrEmpty(videoUrl)) break;
                    }
                }
            }
            else if (output.ValueKind == JsonValueKind.String)
            {
                videoUrl = output.GetString();
            }
        }

        if (string.IsNullOrEmpty(videoUrl))
            throw new InvalidOperationException(
                $"Runway retornou resposta sem URL de vídeo: {responseBody[..Math.Min(300, responseBody.Length)]}");

        return new VideoGenerationResult(videoUrl, null, null);
    }
}
