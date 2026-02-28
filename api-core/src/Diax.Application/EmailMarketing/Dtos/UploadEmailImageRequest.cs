namespace Diax.Application.EmailMarketing.Dtos;

public class UploadEmailImageRequest
{
    /// <summary>
    /// Nome do arquivo da imagem (com extensão).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Conteúdo da imagem em Base64.
    /// </summary>
    public string Base64Content { get; set; } = string.Empty;

    /// <summary>
    /// Tipo MIME da imagem (ex: image/png, image/jpeg).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}
