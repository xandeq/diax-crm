using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Video generation client for Runway ML official API.
/// Supports Gen-4 models with text-to-video and image-to-video capabilities.
/// Uses synchronous HTTP POST to generate endpoint.
/// Auth: Authorization: Bearer {api_key}
/// Docs: https://docs.runwayml.com/api/rest/generate
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
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
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

        var baseUrl = options.BaseUrl ?? "https://api.runwayml.com/v1";
        var endpoint = $"{baseUrl}/image_to_video";

        // Build request payload
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = prompt ?? "A professional video",
            ["model"] = options.Model ?? "gen4",
        };

        // Image-to-video: include reference image
        if (!string.IsNullOrWhiteSpace(referenceImageBase64))
        {
            payload["image"] = referenceImageBase64;
            _logger.LogInformation("[Runway] img2video mode with reference image");
        }

        // Duration (Runway Gen-4 supports 5-30 seconds for free tier)
        if (options.DurationSeconds.HasValue)
        {
            payload["duration"] = Math.Min(options.DurationSeconds.Value, 30);
        }

        // Aspect ratio (if provided)
        if (!string.IsNullOrWhiteSpace(options.AspectRatio))
        {
            payload["aspect_ratio"] = options.AspectRatio;
        }

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        _logger.LogInformation("[Runway] Generating video: model={Model}, duration={Duration}s",
            options.Model ?? "gen4", options.DurationSeconds ?? 5);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "[Runway] Timeout generating video");
            throw new InvalidOperationException(
                "Timeout ao gerar vídeo com Runway. Tente novamente.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[Runway] Error {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody[..Math.Min(500, responseBody.Length)]);

            // Handle specific errors
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException(
                    "Limite diário de geração atingido no Runway (3 vídeos/dia para free tier). " +
                    "Tente novamente amanhã.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    "API key inválida ou expirada no Runway. Verifique as credenciais.");
            }

            throw new InvalidOperationException(
                $"Erro ao gerar vídeo com Runway. Status: {(int)response.StatusCode}. " +
                $"Detalhes: {responseBody[..Math.Min(500, responseBody.Length)]}");
        }

        return ParseResponse(responseBody);
    }

    private static VideoGenerationResult ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        string? videoUrl = null;
        string? thumbnailUrl = null;
        int? durationMs = null;

        // Runway response structure: { "video": "...", "thumbnail": "..." }
        if (root.TryGetProperty("video", out var videoProp))
            videoUrl = videoProp.GetString();

        if (root.TryGetProperty("thumbnail", out var thumbProp))
            thumbnailUrl = thumbProp.GetString();

        if (root.TryGetProperty("duration", out var durProp) && durProp.TryGetInt32(out var dur))
            durationMs = dur * 1000;

        if (string.IsNullOrEmpty(videoUrl))
            throw new InvalidOperationException(
                $"Runway retornou resposta sem URL de vídeo: {responseBody[..Math.Min(300, responseBody.Length)]}");

        return new VideoGenerationResult(videoUrl, thumbnailUrl, durationMs);
    }
}
