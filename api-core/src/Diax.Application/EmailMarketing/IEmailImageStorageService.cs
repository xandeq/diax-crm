using Diax.Application.EmailMarketing.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.EmailMarketing;

/// <summary>
/// Serviço para gerenciar storage de imagens de email.
/// </summary>
public interface IEmailImageStorageService
{
    /// <summary>
    /// Salva uma imagem e retorna a URL pública.
    /// </summary>
    Task<Result<UploadEmailImageResponse>> SaveImageAsync(
        UploadEmailImageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma imagem do storage.
    /// </summary>
    Task<Result> DeleteImageAsync(string imageId, CancellationToken cancellationToken = default);
}
