using Diax.Application.AI.MediaStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Storage local de mídia gerada por IA (mesmo padrão do EmailImageStorageService):
/// arquivos em wwwroot/generated-media servidos como estáticos, URL absoluta via HttpContext.
/// </summary>
public class GeneratedMediaStorageService : IGeneratedMediaStorageService
{
    private const string MediaFolder = "generated-media";
    private const int MaxDownloadBytes = 25 * 1024 * 1024; // 25MB — imagens de IA raramente passam de 10MB

    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp"
    };

    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GeneratedMediaStorageService> _logger;

    public GeneratedMediaStorageService(
        HttpClient httpClient,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GeneratedMediaStorageService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string?> TrySaveImageAsync(
        string imageUrlOrBase64,
        bool isBase64,
        Guid mediaId,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrlOrBase64))
                return null;

            byte[] bytes;
            string extension;

            if (isBase64 || imageUrlOrBase64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                (bytes, extension) = DecodeBase64Payload(imageUrlOrBase64);
            }
            else if (imageUrlOrBase64.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var downloaded = await DownloadAsync(imageUrlOrBase64, ct);
                if (downloaded == null) return null;
                (bytes, extension) = downloaded.Value;
            }
            else
            {
                _logger.LogWarning(
                    "[GeneratedMediaStorage] Formato não reconhecido para media {MediaId} (nem base64, nem URL)", mediaId);
                return null;
            }

            if (bytes.Length == 0)
            {
                _logger.LogWarning("[GeneratedMediaStorage] Conteúdo vazio para media {MediaId}", mediaId);
                return null;
            }

            var fileName = $"{mediaId:N}{extension}";
            var folderPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, MediaFolder);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            await File.WriteAllBytesAsync(filePath, bytes, ct);

            _logger.LogInformation(
                "[GeneratedMediaStorage] Imagem {MediaId} salva ({SizeKB} KB) em {FileName}",
                mediaId, bytes.Length / 1024, fileName);

            return BuildPublicUrl(fileName);
        }
        catch (Exception ex)
        {
            // Nunca derruba a geração por falha de storage — o chamador decide o fallback.
            _logger.LogError(ex, "[GeneratedMediaStorage] Falha ao salvar media {MediaId}", mediaId);
            return null;
        }
    }

    private (byte[] Bytes, string Extension) DecodeBase64Payload(string payload)
    {
        var extension = ".png";
        var base64Content = payload;

        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = payload.IndexOf(',');
            if (commaIndex < 0)
                throw new FormatException("Data URI sem separador ','.");

            var header = payload[5..commaIndex]; // ex: "image/png;base64"
            var mime = header.Split(';')[0];
            if (MimeToExtension.TryGetValue(mime, out var ext))
                extension = ext;

            base64Content = payload[(commaIndex + 1)..];
        }

        return (Convert.FromBase64String(base64Content), extension);
    }

    private async Task<(byte[] Bytes, string Extension)?> DownloadAsync(string url, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "[GeneratedMediaStorage] Download falhou com HTTP {Status} para {Url}",
                (int)response.StatusCode, url);
            return null;
        }

        if (response.Content.Headers.ContentLength is > MaxDownloadBytes)
        {
            _logger.LogWarning(
                "[GeneratedMediaStorage] Download excede limite de {Max} bytes: {Url}",
                MaxDownloadBytes, url);
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length > MaxDownloadBytes)
        {
            _logger.LogWarning("[GeneratedMediaStorage] Conteúdo baixado excede limite: {Url}", url);
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
        var extension = MimeToExtension.TryGetValue(contentType, out var ext) ? ext : ".png";
        return (bytes, extension);
    }

    private string BuildPublicUrl(string fileName)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var scheme = httpContext.Request.Scheme;
            var host = httpContext.Request.Host.Value;
            var pathBase = httpContext.Request.PathBase.Value;
            return $"{scheme}://{host}{pathBase}/{MediaFolder}/{fileName}";
        }

        return $"/{MediaFolder}/{fileName}";
    }
}
