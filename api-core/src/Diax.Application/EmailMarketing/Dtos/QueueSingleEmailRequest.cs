namespace Diax.Application.EmailMarketing.Dtos;

public class QueueSingleEmailRequest
{
    public Guid? CustomerId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public List<EmailAttachmentRequestDto>? Attachments { get; set; }
}
