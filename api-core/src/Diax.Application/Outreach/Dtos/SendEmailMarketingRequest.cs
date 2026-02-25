using Diax.Application.EmailMarketing.Dtos;

namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// Request para envio de email marketing em massa para todos os contatos com email válido.
/// </summary>
public class SendEmailMarketingRequest
{
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public List<EmailAttachmentRequestDto>? Attachments { get; set; }
}
