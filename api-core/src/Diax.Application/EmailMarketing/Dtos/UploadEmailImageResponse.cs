namespace Diax.Application.EmailMarketing.Dtos;

public class UploadEmailImageResponse
{
    /// <summary>
    /// ID único da imagem gerada.
    /// </summary>
    public string ImageId { get; set; } = string.Empty;

    /// <summary>
    /// URL pública absoluta para acessar a imagem.
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// Nome do arquivo salvo.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do arquivo em bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Tipo MIME da imagem.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}
