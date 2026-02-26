using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for fal.ai.
/// Uses the Queue API: POST to queue.fal.run/{model_id}, then polls status via GET.
/// Auth: Authorization: Key {api_key}  (NOT Bearer)
/// Docs: https://fal.ai/docs/model-endpoints/queue
/// </summary>
public class FalAiImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FalAiImageClient> _logger;

    public string ProviderName => "falai";

    public bool SupportsImageToImage => true;

    public FalAiImageClient(HttpClient httpClient, ILogger<FalAiImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Per-request timeout (polling is loop-based)
        _logger = logger;
    }

    public async Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for fal.ai.");

        // Model ID is used directly as part of the URL path
        // e.g. "fal-ai/flux/dev" → https://queue.fal.run/fal-ai/flux/dev
        var modelId = options.Model;
        var submitEndpoint = $"https://queue.fal.run/{modelId}";

        // Build the input payload — sent DIRECTLY (not wrapped in {model, input})
        var inputPayload = new Dictionary<string, object>
        {
            ["prompt"] = prompt ?? string.Empty
        };

        // Image-to-image: pass reference as data URL
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            inputPayload["image_url"] = $"data:image/png;base64,{referenceImageBase64}";
            inputPayload["strength"] = 0.8;
            _logger.LogInformation("[Fal.ai] Image-to-image mode: reference image included");
        }

        // Dimensions (fal.ai accepts image_size as object or named presets)
        if (options.Width > 0 && options.Height > 0)
        {
            inputPayload["image_size"] = new { width = options.Width, height = options.Height };
        }

        if (options.Seed != null && int.TryParse(options.Seed, out var seedInt))
        {
            inputPayload["seed"] = seedInt;
        }

        var json = JsonSerializer.Serialize(inputPayload);

        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, submitEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Fal.ai auth: "Authorization: Key {api_key}"  (not Bearer)
        submitRequest.Headers.Authorization = new AuthenticationHeaderValue("Key", options.ApiKey);

        _logger.LogInformation("[Fal.ai] Submitting to queue: {Endpoint}", submitEndpoint);

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Fal.ai] Submit error {StatusCode}: {Body}", (int)submitResponse.StatusCode, submitBody);
            throw new InvalidOperationException(
                $"Falha ao submeter geração de imagem na fal.ai. Status: {(int)submitResponse.StatusCode}. Detalhes: {submitBody}");
        }

        using var submitDoc = JsonDocument.Parse(submitBody);

        // If the model responded synchronously (no queuing), parse result directly
        if (!submitDoc.RootElement.TryGetProperty("request_id", out var requestIdProp))
        {
            _logger.LogInformation("[Fal.ai] Synchronous response received, parsing directly");
            return ParseResponse(submitBody);
        }

        var requestId = requestIdProp.GetString();
        if (string.IsNullOrEmpty(requestId))
            throw new InvalidOperationException("Fal.ai queue response did not contain a valid request_id.");

        // Use status_url and response_url from the response if available
        var statusUrl = submitDoc.RootElement.TryGetProperty("status_url", out var statusUrlProp)
            ? statusUrlProp.GetString() ?? $"https://queue.fal.run/{modelId}/requests/{requestId}/status"
            : $"https://queue.fal.run/{modelId}/requests/{requestId}/status";

        var resultUrl = submitDoc.RootElement.TryGetProperty("response_url", out var resultUrlProp)
            ? resultUrlProp.GetString() ?? $"https://queue.fal.run/{modelId}/requests/{requestId}"
            : $"https://queue.fal.run/{modelId}/requests/{requestId}";

        _logger.LogInformation("[Fal.ai] Request queued. ID: {RequestId}. Polling status...", requestId);

        return await PollForResultAsync(requestId, options.ApiKey, statusUrl, resultUrl, ct);
    }

    private async Task<List<ImageGenerationResult>> PollForResultAsync(
        string requestId,
        string apiKey,
        string statusUrl,
        string resultUrl,
        CancellationToken ct)
    {
        const int maxAttempts = 60; // 60 × 2s = 120s max
        const int delayMs = 2000;

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            // GET status — fal.ai status endpoint uses GET with path-based request ID
            using var statusReq = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);

            using var statusRes = await _httpClient.SendAsync(statusReq, ct);
            var statusBody = await statusRes.Content.ReadAsStringAsync(ct);

            if (!statusRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Fal.ai] Status check error {StatusCode}: {Body}", (int)statusRes.StatusCode, statusBody);
                throw new InvalidOperationException(
                    $"Falha ao verificar status na fal.ai. Status: {(int)statusRes.StatusCode}");
            }

            using var statusDoc = JsonDocument.Parse(statusBody);
            var status = statusDoc.RootElement.TryGetProperty("status", out var s)
                ? s.GetString()
                : "UNKNOWN";

            _logger.LogDebug("[Fal.ai] Poll {Attempt}/{Max}: status = {Status}", i + 1, maxAttempts, status);

            if (status == "COMPLETED")
            {
                _logger.LogInformation("[Fal.ai] Generation completed for request {RequestId}. Fetching result...", requestId);

                // GET result — also a GET with path-based request ID
                using var resultReq = new HttpRequestMessage(HttpMethod.Get, resultUrl);
                resultReq.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);

                using var resultRes = await _httpClient.SendAsync(resultReq, ct);
                var resultBody = await resultRes.Content.ReadAsStringAsync(ct);

                if (!resultRes.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[Fal.ai] Result fetch error {StatusCode}: {Body}", (int)resultRes.StatusCode, resultBody);
                    throw new InvalidOperationException(
                        $"Falha ao obter resultado da fal.ai. Status: {(int)resultRes.StatusCode}");
                }

                return ParseResponse(resultBody);
            }
            else if (status == "FAILED")
            {
                var errorDetail = statusDoc.RootElement.TryGetProperty("error", out var errProp)
                    ? errProp.GetRawText()
                    : "(sem detalhes)";
                throw new InvalidOperationException(
                    $"Fal.ai reportou falha na geração para o request {requestId}. Erro: {errorDetail}");
            }

            // IN_QUEUE or IN_PROGRESS — wait and retry
            await Task.Delay(delayMs, ct);
        }

        throw new TimeoutException(
            $"Tempo limite excedido aguardando geração de imagem na fal.ai (Request ID: {requestId}). " +
            "Tente novamente ou use um modelo mais rápido como fal-ai/fast-sdxl.");
    }

    private static List<ImageGenerationResult> ParseResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);

        // Fal.ai models return images in different shapes depending on the model.
        // Most common: { "images": [{ "url": "...", "width": ..., "height": ... }] }
        // Some models: { "image": { "url": "..." } }
        // Luma/video: { "video": { "url": "..." } }

        if (doc.RootElement.TryGetProperty("images", out var images))
        {
            foreach (var img in images.EnumerateArray())
            {
                var url = img.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                if (!string.IsNullOrEmpty(url))
                {
                    results.Add(new ImageGenerationResult(
                        ImageUrl: url,
                        IsBase64: false,
                        RevisedPrompt: null,
                        Seed: img.TryGetProperty("seed", out var seedProp) ? seedProp.GetRawText() : null));
                }
            }
        }
        else if (doc.RootElement.TryGetProperty("image", out var image))
        {
            var url = image.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
            if (!string.IsNullOrEmpty(url))
            {
                results.Add(new ImageGenerationResult(
                    ImageUrl: url,
                    IsBase64: false,
                    RevisedPrompt: null,
                    Seed: null));
            }
        }

        return results;
    }
}
