namespace Diax.Application.EmailMarketing.Dtos;

public class QueueBulkEmailRequest
{
    public List<Guid> CustomerIds { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public List<EmailAttachmentRequestDto>? Attachments { get; set; }
}
