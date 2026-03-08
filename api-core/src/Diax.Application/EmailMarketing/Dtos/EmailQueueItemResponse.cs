using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Application.EmailMarketing.Dtos;

public class EmailQueueItemResponse
{
    public Guid Id { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? CustomerId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailQueueStatus Status { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public int ReadCount { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }

    public static EmailQueueItemResponse FromEntity(EmailQueueItem entity)
    {
        return new EmailQueueItemResponse
        {
            Id = entity.Id,
            CampaignId = entity.CampaignId,
            CustomerId = entity.CustomerId,
            RecipientName = entity.RecipientName,
            RecipientEmail = entity.RecipientEmail,
            Subject = entity.Subject,
            Status = entity.Status,
            ScheduledAt = entity.ScheduledAt,
            SentAt = entity.SentAt,
            DeliveredAt = entity.DeliveredAt,
            OpenedAt = entity.OpenedAt,
            ReadCount = entity.ReadCount,
            AttemptCount = entity.AttemptCount,
            LastError = entity.LastError,
            CreatedAt = entity.CreatedAt
        };
    }
}
