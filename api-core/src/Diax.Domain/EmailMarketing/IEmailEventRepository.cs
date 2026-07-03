using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Domain.EmailMarketing;

/// <summary>
/// Ledger de eventos de email (delivered/opened/clicked/bounced/unsubscribed/spam).
/// Fonte de idempotência dos webhooks: um evento por (queue item, tipo).
/// </summary>
public interface IEmailEventRepository
{
    Task<bool> ExistsAsync(Guid queueItemId, EmailEventType eventType, CancellationToken cancellationToken = default);
    Task AddAsync(EmailEvent emailEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agregado de engajamento por cliente (opens/clicks/bounces + último engajamento).
    /// Usado pelo lead scoring. Só considera eventos com CustomerId preenchido.
    /// </summary>
    Task<List<CustomerEngagementSummary>> GetEngagementSummaryAsync(CancellationToken cancellationToken = default);
}
