using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class OpenAiImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiImageClient> _logger;

    public string ProviderName => "openai";
    public bool SupportsImageToImage => true;

    public OpenAiImageClient(HttpClient httpClient, ILogger<OpenAiImageClient> logger)
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
            throw new InvalidOperationException("API key not configured for OpenAI.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://api.openai.com/v1"
            : options.BaseUrl.TrimEnd('/');

        // 1. Variations (Image only, no prompt)
        if (!string.IsNullOrWhiteSpace(referenceImageBase64) && string.IsNullOrWhiteSpace(prompt))
        {
            return await GenerateVariationAsync(options, referenceImageBase64, baseUrl, ct);
        }

        // 2. Edits (Image + Prompt)
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            return await GenerateWithReferenceAsync(prompt!, options, referenceImageBase64, baseUrl, ct);
        }

        // 3. Generations (Text only)
        return await GenerateFromTextAsync(prompt!, options, baseUrl, ct);
    }

    private async Task<List<ImageGenerationResult>> GenerateFromTextAsync(
        string prompt,
        ImageGenerationOptions options,
        string baseUrl,
        CancellationToken ct)
    {
        var endpoint = $"{baseUrl}/images/generations";

        var payload = new Dictionary<string, object>
        {
            ["model"] = options.Model,
            ["prompt"] = prompt,
            ["n"] = options.NumberOfImages,
            ["size"] = $"{options.Width}x{options.Height}",
            ["response_format"] = "url"
        };

        if (!string.IsNullOrWhiteSpace(options.Quality))
            payload["quality"] = options.Quality;

        if (!string.IsNullOrWhiteSpace(options.Style))
            payload["style"] = options.Style;

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        _logger.LogInformation("[OpenAI Image] Generating {Count} image(s) with model {Model}, size {W}x{H}",
            options.NumberOfImages, options.Model, options.Width, options.Height);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[OpenAI Image] API error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na geração de imagem via OpenAI. Status: {(int)response.StatusCode}");
        }

        return ParseResponse(responseBody);
    }

    private async Task<List<ImageGenerationResult>> GenerateWithReferenceAsync(
        string prompt,
        ImageGenerationOptions options,
        string referenceImageBase64,
        string baseUrl,
        CancellationToken ct)
    {
        var endpoint = $"{baseUrl}/images/edits";

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(options.Model), "model");
        content.Add(new StringContent(prompt), "prompt");
        content.Add(new StringContent(options.NumberOfImages.ToString()), "n");
        content.Add(new StringContent($"{options.Width}x{options.Height}"), "size");

        // Convert base64 to byte array for the image field
        var imageBytes = Convert.FromBase64String(referenceImageBase64);
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "reference.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        _logger.LogInformation("[OpenAI Image] Generating img2img with model {Model}", options.Model);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[OpenAI Image] img2img error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na edição de imagem via OpenAI. Status: {(int)response.StatusCode}");
        }

        return ParseResponse(responseBody);
    }

    private async Task<List<ImageGenerationResult>> GenerateVariationAsync(
        ImageGenerationOptions options,
        string referenceImageBase64,
        string baseUrl,
        CancellationToken ct)
    {
        var endpoint = $"{baseUrl}/images/variations";

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(options.Model), "model");
        content.Add(new StringContent(options.NumberOfImages.ToString()), "n");
        content.Add(new StringContent($"{options.Width}x{options.Height}"), "size");

        // Convert base64 to byte array for the image field
        var imageBytes = Convert.FromBase64String(referenceImageBase64);
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "reference.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        _logger.LogInformation("[OpenAI Image] Generating variations with model {Model}", options.Model);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[OpenAI Image] variations error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Falha na variação de imagem via OpenAI. Status: {(int)response.StatusCode}");
        }

        return ParseResponse(responseBody);
    }

    private static List<ImageGenerationResult> ParseResponse(string responseBody)
    {
        var results = new List<ImageGenerationResult>();

        using var doc = JsonDocument.Parse(responseBody);
        var data = doc.RootElement.GetProperty("data");

        foreach (var item in data.EnumerateArray())
        {
            var url = item.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
            var b64 = item.TryGetProperty("b64_json", out var b64Prop) ? b64Prop.GetString() : null;
            var revisedPrompt = item.TryGetProperty("revised_prompt", out var rpProp) ? rpProp.GetString() : null;

            var imageUrl = url ?? (b64 != null ? $"data:image/png;base64,{b64}" : string.Empty);
            var isBase64 = url == null && b64 != null;

            results.Add(new ImageGenerationResult(
                ImageUrl: imageUrl,
                IsBase64: isBase64,
                RevisedPrompt: revisedPrompt,
                Seed: null));
        }

        return results;
    }
}
