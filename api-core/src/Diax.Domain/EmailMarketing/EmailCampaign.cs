using Diax.Domain.Common;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Domain.EmailMarketing;

public class EmailCampaign : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string BodyHtml { get; private set; } = string.Empty;
    public DateTime? ScheduledAt { get; private set; }
    public EmailCampaignStatus Status { get; private set; }
    public Guid? SourceSnippetId { get; private set; }

    // Statistics
    public int TotalRecipients { get; private set; }
    public int SentCount { get; private set; }
    public int FailedCount { get; private set; }
    public int OpenCount { get; private set; }

    protected EmailCampaign() { }

    public EmailCampaign(
        Guid userId,
        string name,
        string subject,
        string bodyHtml,
        Guid? sourceSnippetId = null)
    {
        UserId = userId;
        Name = name;
        Subject = subject;
        BodyHtml = bodyHtml;
        SourceSnippetId = sourceSnippetId;
        Status = EmailCampaignStatus.Draft;
    }

    public void UpdateContent(string name, string subject, string bodyHtml)
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Can only update campaigns in Draft status.");

        Name = name;
        Subject = subject;
        BodyHtml = bodyHtml;
    }

    public void SetSourceSnippet(Guid? sourceSnippetId)
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Can only update campaign template source in Draft status.");

        SourceSnippetId = sourceSnippetId;
    }

    public void Schedule(DateTime scheduledAt)
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Can only schedule campaigns in Draft status.");

        ScheduledAt = scheduledAt;
        Status = EmailCampaignStatus.Scheduled;
    }

    public void MarkDraft()
    {
        Status = EmailCampaignStatus.Draft;
        ScheduledAt = null;
    }

    public void StartProcessing()
    {
        Status = EmailCampaignStatus.Processing;
    }

    public void MarkCompleted()
    {
        Status = EmailCampaignStatus.Completed;
    }

    public void Cancel()
    {
        Status = EmailCampaignStatus.Cancelled;
    }

    public void SetTotalRecipients(int total)
    {
        TotalRecipients = total;
    }

    // Methods to increment stats (intended to be called by domain/service logic,
    // potentially though specialized updates for concurrency)
    public void IncrementSent()
    {
        SentCount++;
        CheckCompletion();
    }

    public void IncrementFailed()
    {
        FailedCount++;
        CheckCompletion();
    }

    public void IncrementOpened()
    {
        OpenCount++;
    }

    private void CheckCompletion()
    {
        if (TotalRecipients > 0 && (SentCount + FailedCount) >= TotalRecipients)
        {
            Status = EmailCampaignStatus.Completed;
        }
    }
}
