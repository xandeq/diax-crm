using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for Pollinations.ai — 100% gratuito e SEM API key.
/// GET https://image.pollinations.ai/prompt/{prompt}?width=&height=&model=&seed=&nologo=true
/// retorna os bytes da imagem diretamente. Serve como fallback keyless de imagem
/// (nenhuma conta/crédito para expirar). Modelos: flux (default), turbo.
/// </summary>
public class PollinationsImageClient : IAiImageGenerationClient
{
    private const string DefaultBaseUrl = "https://image.pollinations.ai";
    private const int MaxImageBytes = 25 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly ILogger<PollinationsImageClient> _logger;

    public string ProviderName => "pollinations";

    public bool SupportsImageToImage => false;

    public PollinationsImageClient(HttpClient httpClient, ILogger<PollinationsImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120); // geração pode enfileirar no free tier
        _logger = logger;
    }

    public async Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        // Sem API key de propósito — Pollinations é keyless.
        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("Pollinations exige um prompt de texto.");

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? DefaultBaseUrl : options.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(options.Model) ? "flux" : options.Model;

        var results = new List<ImageGenerationResult>();
        var count = Math.Clamp(options.NumberOfImages, 1, 4);

        for (var i = 0; i < count; i++)
        {
            // Seed diferente por imagem para variar o resultado da leva
            var seed = !string.IsNullOrWhiteSpace(options.Seed) && long.TryParse(options.Seed, out var s)
                ? s + i
                : Random.Shared.NextInt64(1, int.MaxValue);

            var url = $"{baseUrl}/prompt/{Uri.EscapeDataString(prompt)}" +
                      $"?width={options.Width}&height={options.Height}" +
                      $"&model={Uri.EscapeDataString(model)}&seed={seed}&nologo=true";

            _logger.LogInformation("[Pollinations] Generating image {Index}/{Count} (model: {Model})", i + 1, count, model);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[Pollinations] HTTP {Status}: {Body}",
                    (int)response.StatusCode, body.Length > 200 ? body[..200] : body);
                throw new InvalidOperationException(
                    $"Falha na geração de imagem via Pollinations. Status: {(int)response.StatusCode}.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Pollinations retornou conteúdo inesperado ({contentType}): {(body.Length > 150 ? body[..150] : body)}");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length == 0 || bytes.Length > MaxImageBytes)
                throw new InvalidOperationException(
                    $"Pollinations retornou imagem inválida ({bytes.Length} bytes).");

            var mime = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
            results.Add(new ImageGenerationResult(
                ImageUrl: $"data:{mime};base64,{Convert.ToBase64String(bytes)}",
                IsBase64: true,
                RevisedPrompt: null,
                Seed: seed.ToString()));
        }

        _logger.LogInformation("[Pollinations] {Count} image(s) generated", results.Count);
        return results;
    }
}
