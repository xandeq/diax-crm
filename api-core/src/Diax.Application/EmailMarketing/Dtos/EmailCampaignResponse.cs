using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Application.EmailMarketing.Dtos;

public class EmailCampaignResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public EmailCampaignStatus Status { get; set; }
    public Guid? SourceSnippetId { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
    public int DeliveredCount { get; set; }
    public int BounceCount { get; set; }
    public int UnsubscribeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static EmailCampaignResponse FromEntity(EmailCampaign entity)
    {
        return new EmailCampaignResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Subject = entity.Subject,
            BodyHtml = entity.BodyHtml,
            ScheduledAt = entity.ScheduledAt,
            Status = entity.Status,
            SourceSnippetId = entity.SourceSnippetId,
            TotalRecipients = entity.TotalRecipients,
            SentCount = entity.SentCount,
            FailedCount = entity.FailedCount,
            OpenCount = entity.OpenCount,
            ClickCount = entity.ClickCount,
            DeliveredCount = entity.DeliveredCount,
            BounceCount = entity.BounceCount,
            UnsubscribeCount = entity.UnsubscribeCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
