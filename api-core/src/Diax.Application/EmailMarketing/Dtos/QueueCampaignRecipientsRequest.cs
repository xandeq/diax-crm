namespace Diax.Application.EmailMarketing.Dtos;

public class QueueCampaignRecipientsRequest
{
    public List<Guid> CustomerIds { get; set; } = [];
    public DateTime? ScheduledAt { get; set; }
    public string? BodyHtmlOverride { get; set; }
    public List<EmailAttachmentRequestDto>? Attachments { get; set; }
}
