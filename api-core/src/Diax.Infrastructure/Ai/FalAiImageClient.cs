using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for fal.ai.
/// Supports both text-to-image and image-to-image.
/// Fal.ai endpoints expect JSON with "prompt" and optionally "image_url" (can be data URL).
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
            throw new InvalidOperationException("API key not configured for fal.ai.");

        // Fal.ai model keys on our side should match their IDs, e.g., "fal-ai/flux/dev"
        var modelId = options.Model.Replace(":", "/"); // Allow colon for some providers if needed
        
        // We will use the queue endpoint to avoid timeouts on large models,
        // but we will poll synchronously to keep the architecture unchanged.
        var endpoint = "https://queue.fal.run/v1";

        var inputPayload = new Dictionary<string, object>
        {
            ["prompt"] = prompt ?? string.Empty
        };

        // Add reference image if provided (as data URL)
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            inputPayload["image_url"] = $"data:image/png;base64,{referenceImageBase64}";
            // Some models use "strength" for image-to-image
            inputPayload["strength"] = 0.8;
            _logger.LogInformation("[Fal.ai] Image-to-image mode: reference image included");
        }

        // Add additional common parameters if present in options
        if (options.Width > 0 && options.Height > 0)
        {
            inputPayload["width"] = options.Width;
            inputPayload["height"] = options.Height;
        }

        if (options.Seed != null && int.TryParse(options.Seed, out var seedInt))
        {
            inputPayload["seed"] = seedInt;
        }

        var payload = new Dictionary<string, object>
        {
            ["model"] = modelId,
            ["input"] = inputPayload
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Fal.ai supports Bearer token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        _logger.LogInformation("[Fal.ai] Submitting generation request to queue for model {Model}", options.Model);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Fal.ai] API error {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"Falha na geração de imagem via fal.ai. Status: {(int)response.StatusCode}");
        }

        // Parse request_id
        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("request_id", out var requestIdProp))
        {
            // If it returned the result directly (synchronous fallback)
            return ParseResponse(responseBody);
        }

        var requestId = requestIdProp.GetString();
        if (string.IsNullOrEmpty(requestId))
            throw new InvalidOperationException("Fal.ai queue response did not contain a valid request_id.");

        _logger.LogInformation("[Fal.ai] Request queued with ID {RequestId}. Polling for completion...", requestId);

        // Poll for completion
        return await PollForResultAsync(requestId, options.ApiKey, ct);
    }

    private async Task<List<ImageGenerationResult>> PollForResultAsync(string requestId, string apiKey, CancellationToken ct)
    {
        var statusEndpoint = "https://queue.fal.run/v1/status";
        var resultEndpoint = "https://queue.fal.run/v1/result";
        
        var payload = new Dictionary<string, string> { ["requestId"] = requestId };
        var json = JsonSerializer.Serialize(payload);

        int maxAttempts = 60; // 60 attempts * 2 seconds = 120 seconds max
        int delayMs = 2000;

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            using var statusReq = new HttpRequestMessage(HttpMethod.Post, statusEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var statusRes = await _httpClient.SendAsync(statusReq, ct);
            var statusBody = await statusRes.Content.ReadAsStringAsync(ct);

            if (!statusRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Fal.ai] Status check failed {StatusCode}: {Body}", (int)statusRes.StatusCode, statusBody);
                throw new InvalidOperationException($"Falha ao verificar status na fal.ai. Status: {(int)statusRes.StatusCode}");
            }

            using var statusDoc = JsonDocument.Parse(statusBody);
            var status = statusDoc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : "UNKNOWN";

            if (status == "COMPLETED")
            {
                _logger.LogInformation("[Fal.ai] Generation completed for request {RequestId}. Fetching result...", requestId);
                
                using var resultReq = new HttpRequestMessage(HttpMethod.Post, resultEndpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                resultReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var resultRes = await _httpClient.SendAsync(resultReq, ct);
                var resultBody = await resultRes.Content.ReadAsStringAsync(ct);

                if (!resultRes.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Falha ao obter resultado da fal.ai. Status: {(int)resultRes.StatusCode}");
                }

                return ParseResponse(resultBody);
            }
            else if (status == "FAILED")
            {
                throw new InvalidOperationException($"Fal.ai reportou falha na geração para o request {requestId}.");
            }

            // IN_QUEUE or IN_PROGRESS
            await Task.Delay(delayMs, ct);
        }

        throw new TimeoutException($"Tempo limite excedido aguardando a geração da imagem na fal.ai (Request ID: {requestId}).");
    }

    private static List<ImageGenerationResult> ParseResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);

        // Fal.ai usually returns an object with "images" array or single "image"
        if (doc.RootElement.TryGetProperty("images", out var images))
        {
            foreach (var img in images.EnumerateArray())
            {
                if (img.TryGetProperty("url", out var urlProp))
                {
                    results.Add(new ImageGenerationResult(
                        ImageUrl: urlProp.GetString() ?? string.Empty,
                        IsBase64: false,
                        RevisedPrompt: null,
                        Seed: null));
                }
            }
        }
        else if (doc.RootElement.TryGetProperty("image", out var image))
        {
            if (image.TryGetProperty("url", out var urlProp))
            {
                results.Add(new ImageGenerationResult(
                    ImageUrl: urlProp.GetString() ?? string.Empty,
                    IsBase64: false,
                    RevisedPrompt: null,
                    Seed: null));
            }
        }

        return results;
    }
}
