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
        // The endpoint is usually https://fal.run/{model_id}
        var modelId = options.Model.Replace(":", "/"); // Allow colon for some providers if needed
        var endpoint = $"https://fal.run/{modelId}";

        var payload = new Dictionary<string, object>
        {
            ["prompt"] = prompt ?? string.Empty
        };

        // Add reference image if provided (as data URL)
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            // Fal.ai usually accepts data URLs. Assuming PNG/JPEG.
            payload["image_url"] = $"data:image/png;base64,{referenceImageBase64}";
            _logger.LogInformation("[Fal.ai] Image-to-image mode: reference image included");
        }

        // Add additional common parameters if present in options
        if (options.Width > 0 && options.Height > 0)
        {
            // Some models use "image_size" enum or custom width/height
            // Flux on Fal uses "image_size" or "width"/"height"
            payload["width"] = options.Width;
            payload["height"] = options.Height;
        }

        if (options.Seed != null)
        {
            if (int.TryParse(options.Seed, out var seedInt))
            {
                payload["seed"] = seedInt;
            }
        }

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Fal.ai uses "Key {API_KEY}" format instead of "Bearer"
        request.Headers.Authorization = new AuthenticationHeaderValue("Key", options.ApiKey);

        _logger.LogInformation("[Fal.ai] Generating image(s) with model {Model}", options.Model);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Fal.ai] API error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na geração de imagem via fal.ai. Status: {(int)response.StatusCode}");
        }

        return ParseResponse(responseBody);
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
