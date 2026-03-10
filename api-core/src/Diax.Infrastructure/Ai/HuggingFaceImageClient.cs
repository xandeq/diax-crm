using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for Hugging Face Inference Router API.
/// Calls POST https://router.huggingface.co/hf-inference/models/{model_id}
/// Auth: Authorization: Bearer {hf_token}
/// Response: binary PNG → converted to base64 data URL
/// Free models: black-forest-labs/FLUX.1-schnell, stabilityai/stable-diffusion-xl-base-1.0, etc.
/// Docs: https://huggingface.co/docs/api-inference/tasks/text-to-image
/// </summary>
public class HuggingFaceImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceImageClient> _logger;

    public string ProviderName => "huggingface";
    public bool SupportsImageToImage => false;

    // Models that need special input format
    private static readonly HashSet<string> StructuredInputModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "black-forest-labs/FLUX.1-schnell",
        "black-forest-labs/FLUX.1-dev",
        "stabilityai/stable-diffusion-3-medium-diffusers",
        "stabilityai/stable-diffusion-xl-base-1.0",
    };

    public HuggingFaceImageClient(HttpClient httpClient, ILogger<HuggingFaceImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120); // HF can be slow on cold start
        _logger = logger;
    }

    public async Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for Hugging Face.");

        var modelId = options.Model;
        var endpoint = $"https://router.huggingface.co/hf-inference/models/{modelId}";

        // HF Inference API accepts JSON with "inputs" key
        var payload = new Dictionary<string, object>
        {
            ["inputs"] = prompt ?? string.Empty
        };

        // Add parameters if supported by structured models
        if (StructuredInputModels.Contains(modelId))
        {
            var parameters = new Dictionary<string, object>();
            if (options.Width > 0 && options.Height > 0)
            {
                parameters["width"] = options.Width;
                parameters["height"] = options.Height;
            }
            if (!string.IsNullOrWhiteSpace(options.NegativePrompt))
                parameters["negative_prompt"] = options.NegativePrompt;
            if (parameters.Count > 0)
                payload["parameters"] = parameters;
        }

        var json = JsonSerializer.Serialize(payload);
        _logger.LogInformation("[HuggingFace] Generating image: model={Model}", modelId);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        // Ask for JSON response format where supported
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "[HuggingFace] Timeout gerando imagem com modelo {Model}", modelId);
            throw new InvalidOperationException(
                $"Timeout ao gerar imagem com HuggingFace ({_httpClient.Timeout.TotalSeconds}s). Modelos gratuitos podem demorar em cold start — tente novamente em instantes.");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[HuggingFace] Error {StatusCode} for model {Model}: {Body}", (int)response.StatusCode, modelId, errorBody);

            // 503: model is loading (cold start) — very common on free tier
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                // Try to parse estimated_time from response
                string? estimatedTime = null;
                try
                {
                    using var errDoc = JsonDocument.Parse(errorBody);
                    if (errDoc.RootElement.TryGetProperty("estimated_time", out var et))
                        estimatedTime = $" ({et.GetDouble():F0}s estimados)";
                }
                catch { }
                throw new InvalidOperationException(
                    $"Modelo '{modelId}' está carregando no HuggingFace{estimatedTime}. " +
                    "Isso é normal na primeira chamada (cold start gratuito) — aguarde alguns segundos e tente novamente.");
            }

            // 429: rate limit
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException(
                    $"Rate limit do HuggingFace atingido para o modelo '{modelId}'. " +
                    "Aguarde 60s e tente novamente, ou troque para outro modelo. Causa: {errorBody}");
            }

            // 401/403: auth issues
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    $"API key inválida ou sem permissão para o modelo '{modelId}' no HuggingFace. " +
                    "Verifique o token em Administração > AI > Providers.");
            }

            throw new InvalidOperationException(
                $"Erro ao gerar imagem com HuggingFace (modelo: {modelId}). Status: {(int)response.StatusCode}. Causa: {errorBody}");
        }

        // Response is binary image data (PNG/JPEG)
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
            var base64 = Convert.ToBase64String(imageBytes);
            _logger.LogInformation("[HuggingFace] Image generated ({Bytes} bytes)", imageBytes.Length);
            return new List<ImageGenerationResult>
            {
                new(base64, true, null, null)
            };
        }

        // Some models return JSON
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return ParseJsonResponse(body);
        }

        // Fallback: treat raw bytes as image
        var rawBytes = await response.Content.ReadAsByteArrayAsync(ct);
        var rawBase64 = Convert.ToBase64String(rawBytes);
        return new List<ImageGenerationResult> { new(rawBase64, true, null, null) };
    }

    private static List<ImageGenerationResult> ParseJsonResponse(string body)
    {
        var results = new List<ImageGenerationResult>();
        using var doc = JsonDocument.Parse(body);

        // Some HF models return [{"generated_text":"..."}] or [{"image":"base64..."}]
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("image", out var imgProp))
                {
                    var b64 = imgProp.GetString();
                    if (!string.IsNullOrEmpty(b64))
                        results.Add(new ImageGenerationResult(b64, true, null, null));
                }
            }
        }

        return results;
    }
}
