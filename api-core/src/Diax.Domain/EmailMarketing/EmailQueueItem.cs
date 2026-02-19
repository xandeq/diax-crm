using Diax.Domain.Common;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Domain.EmailMarketing;

public class EmailQueueItem : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string? AttachmentsJson { get; private set; }
    public EmailQueueStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public Guid? CampaignId { get; private set; }

    protected EmailQueueItem()
    {
    }

    public EmailQueueItem(
        Guid userId,
        string recipientName,
        string recipientEmail,
        string subject,
        string htmlBody,
        DateTime scheduledAt,
        Guid? customerId = null,
        string? attachmentsJson = null,
        Guid? campaignId = null)
    {
        UserId = userId;
        CustomerId = customerId;
        RecipientName = recipientName;
        RecipientEmail = recipientEmail;
        Subject = subject;
        HtmlBody = htmlBody;
        ScheduledAt = scheduledAt;
        AttachmentsJson = attachmentsJson;
        CampaignId = campaignId;
        Status = EmailQueueStatus.Queued;
    }

    public void MarkProcessing()
    {
        Status = EmailQueueStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
        AttemptCount++;
        LastError = null;
        SetUpdated("system");
    }

    public void MarkSent(string? providerMessageId = null)
    {
        Status = EmailQueueStatus.Sent;
        SentAt = DateTime.UtcNow;
        ProviderMessageId = providerMessageId;
        LastError = null;
        SetUpdated("system");
    }

    public void MarkFailed(string errorMessage)
    {
        Status = EmailQueueStatus.Failed;
        LastError = errorMessage;
        SetUpdated("system");
    }
}
