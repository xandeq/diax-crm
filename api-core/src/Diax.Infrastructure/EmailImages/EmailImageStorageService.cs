using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Shared.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.EmailImages;

public class EmailImageStorageService : IEmailImageStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EmailImageStorageService> _logger;
    private const string EmailImagesFolder = "email-images";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };
    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp"
    };

    public EmailImageStorageService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EmailImageStorageService> logger)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<UploadEmailImageResponse>> SaveImageAsync(
        UploadEmailImageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validações
            if (string.IsNullOrWhiteSpace(request.Base64Content))
            {
                return Result.Failure<UploadEmailImageResponse>(
                    Error.Validation("Base64Content", "Conteúdo da imagem é obrigatório."));
            }

            if (string.IsNullOrWhiteSpace(request.ContentType))
            {
                return Result.Failure<UploadEmailImageResponse>(
                    Error.Validation("ContentType", "Tipo MIME da imagem é obrigatório."));
            }

            // Validar tipo MIME
            if (!MimeToExtension.ContainsKey(request.ContentType))
            {
                return Result.Failure<UploadEmailImageResponse>(
                    Error.Validation("ContentType",
                        $"Tipo de imagem não suportado: {request.ContentType}. " +
                        "Tipos aceitos: image/jpeg, image/png, image/gif, image/webp"));
            }

            // Decodificar Base64
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(request.Base64Content);
            }
            catch (FormatException)
            {
                return Result.Failure<UploadEmailImageResponse>(
                    Error.Validation("Base64Content", "Conteúdo Base64 inválido."));
            }

            // Validar tamanho (max 5MB)
            const int maxSizeBytes = 5 * 1024 * 1024;
            if (imageBytes.Length > maxSizeBytes)
            {
                return Result.Failure<UploadEmailImageResponse>(
                    Error.Validation("Base64Content",
                        $"Imagem muito grande ({imageBytes.Length / 1024} KB). Máximo: 5 MB."));
            }

            // Gerar nome único do arquivo
            var extension = MimeToExtension[request.ContentType];
            var imageId = Guid.NewGuid().ToString("N");
            var fileName = $"{imageId}{extension}";

            // Criar pasta se não existir
            var imagesPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, EmailImagesFolder);
            Directory.CreateDirectory(imagesPath);

            // Salvar arquivo
            var filePath = Path.Combine(imagesPath, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

            _logger.LogInformation(
                "Imagem de email salva: {FileName} ({SizeKB} KB)",
                fileName,
                imageBytes.Length / 1024);

            // Construir URL pública ABSOLUTA (obrigatório para emails)
            // CRÍTICO: Emails precisam de URL completa com domínio, não relativa
            var httpContext = _httpContextAccessor.HttpContext;
            var publicUrl = $"/email-images/{fileName}";

            if (httpContext != null)
            {
                var scheme = httpContext.Request.Scheme;
                var host = httpContext.Request.Host.Value;
                var pathBase = httpContext.Request.PathBase.Value;
                publicUrl = $"{scheme}://{host}{pathBase}/email-images/{fileName}";
            }

            return new UploadEmailImageResponse
            {
                ImageId = imageId,
                PublicUrl = publicUrl,
                FileName = fileName,
                FileSizeBytes = imageBytes.Length,
                ContentType = request.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar imagem de email.");
            return Result.Failure<UploadEmailImageResponse>(
                new Error("EmailImage.SaveFailed", $"Erro ao salvar imagem: {ex.Message}"));
        }
    }

    public Task<Result> DeleteImageAsync(string imageId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageId))
            {
                return Task.FromResult(Result.Failure(Error.Validation("ImageId", "ID da imagem é obrigatório.")));
            }

            var imagesPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, EmailImagesFolder);

            // Buscar arquivo com este ID (qualquer extensão permitida)
            foreach (var ext in AllowedExtensions)
            {
                var fileName = $"{imageId}{ext}";
                var filePath = Path.Combine(imagesPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Imagem de email removida: {FileName}", fileName);
                    return Task.FromResult(Result.Success());
                }
            }

            return Task.FromResult(Result.Failure(Error.NotFound("EmailImage", imageId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover imagem de email: {ImageId}", imageId);
            return Task.FromResult(Result.Failure(new Error("EmailImage.DeleteFailed", $"Erro ao remover imagem: {ex.Message}")));
        }
    }
}
