namespace Diax.Application.EmailMarketing.Dtos;

public class EmailAttachmentRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string Base64Content { get; set; } = string.Empty;
}
