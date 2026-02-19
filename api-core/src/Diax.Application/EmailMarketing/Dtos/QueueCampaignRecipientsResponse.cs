namespace Diax.Application.EmailMarketing.Dtos;

public class QueueCampaignRecipientsResponse
{
    public Guid CampaignId { get; set; }
    public int RequestedCount { get; set; }
    public int QueuedCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime EffectiveScheduledAt { get; set; }
    public List<string> SkippedRecipients { get; set; } = [];
}
