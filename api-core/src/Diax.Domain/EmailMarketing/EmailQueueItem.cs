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
    public EmailProvider AssignedProvider { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? OpenedAt { get; private set; }
    public int ReadCount { get; private set; }

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
        Guid? campaignId = null,
        EmailProvider assignedProvider = EmailProvider.Brevo)
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
        AssignedProvider = assignedProvider;
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

    public void MarkDelivered()
    {
        DeliveredAt = DateTime.UtcNow;
        SetUpdated("system");
    }

    public void RecordOpen()
    {
        OpenedAt = DateTime.UtcNow;
        ReadCount++;
        SetUpdated("system");
    }

    public void Requeue(DateTime retryAt)
    {
        Status = EmailQueueStatus.Queued;
        ScheduledAt = retryAt;
        // LastError é preservado de propósito: enquanto o item aguarda retry,
        // a causa da última falha precisa continuar visível para diagnóstico.
        ProcessingStartedAt = null;
        SetUpdated("system");
    }

    /// <summary>
    /// Troca o provider do item (usado no retry para não insistir em provider que falhou
    /// e para tirar itens de providers desabilitados/sem credencial).
    /// </summary>
    public void ReassignProvider(EmailProvider provider)
    {
        AssignedProvider = provider;
        SetUpdated("system");
    }

    /// <summary>
    /// Tentativas esgotadas — sai da rotação de retry até intervenção manual.
    /// </summary>
    public void MarkDeadLettered(string reason)
    {
        Status = EmailQueueStatus.DeadLettered;
        LastError = reason;
        ProcessingStartedAt = null;
        SetUpdated("system");
    }

    /// <summary>
    /// Cancelamento em tempo de despacho: supressão/opt-out detectados DEPOIS do
    /// enqueue (o enqueue já filtra, mas bounce/unsubscribe podem chegar entre o
    /// agendamento e o envio). Terminal — não incrementa AttemptCount, não entra
    /// no retry nem na DLQ.
    /// </summary>
    public void MarkCancelled(string reason)
    {
        Status = EmailQueueStatus.Cancelled;
        LastError = reason;
        ProcessingStartedAt = null;
        SetUpdated("system");
    }

    /// <summary>
    /// Requeue manual a partir da DLQ: zera as tentativas (ganha um ciclo completo
    /// de retries) e volta para a fila imediatamente.
    /// </summary>
    public void RestoreFromDeadLetter(DateTime utcNow)
    {
        Status = EmailQueueStatus.Queued;
        ScheduledAt = utcNow;
        AttemptCount = 0;
        ProcessingStartedAt = null;
        // LastError preservado até o próximo MarkProcessing — diagnóstico do motivo original.
        SetUpdated("system");
    }
}
