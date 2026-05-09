using Diax.Domain.Common;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Domain.EmailMarketing;

public class EmailEvent : AuditableEntity
{
    public Guid? QueueItemId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public Guid UserId { get; private set; }
    public EmailProvider Provider { get; private set; }
    public EmailEventType EventType { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? Metadata { get; private set; }
    public DateTime OccurredAt { get; private set; }

    protected EmailEvent() { }

    public EmailEvent(
        Guid userId,
        EmailProvider provider,
        EmailEventType eventType,
        Guid? queueItemId = null,
        Guid? customerId = null,
        Guid? campaignId = null,
        string? providerMessageId = null,
        string? metadata = null)
    {
        UserId = userId;
        Provider = provider;
        EventType = eventType;
        QueueItemId = queueItemId;
        CustomerId = customerId;
        CampaignId = campaignId;
        ProviderMessageId = providerMessageId;
        Metadata = metadata;
        OccurredAt = DateTime.UtcNow;
    }
}
