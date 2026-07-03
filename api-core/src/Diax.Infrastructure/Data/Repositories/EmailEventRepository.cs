using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class EmailEventRepository : IEmailEventRepository
{
    private readonly DiaxDbContext _db;

    public EmailEventRepository(DiaxDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(Guid queueItemId, EmailEventType eventType, CancellationToken cancellationToken = default)
    {
        return await _db.EmailEvents
            .AnyAsync(e => e.QueueItemId == queueItemId && e.EventType == eventType, cancellationToken);
    }

    public async Task AddAsync(EmailEvent emailEvent, CancellationToken cancellationToken = default)
    {
        await _db.EmailEvents.AddAsync(emailEvent, cancellationToken);
    }

    public async Task<List<CustomerEngagementSummary>> GetEngagementSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        // Agregação no servidor usando o índice (customer_id, event_type)
        return await _db.EmailEvents
            .Where(e => e.CustomerId != null)
            .GroupBy(e => e.CustomerId!.Value)
            .Select(g => new CustomerEngagementSummary(
                g.Key,
                g.Count(e => e.EventType == EmailEventType.Opened),
                g.Count(e => e.EventType == EmailEventType.Clicked),
                g.Count(e => e.EventType == EmailEventType.Bounced),
                g.Where(e => e.EventType == EmailEventType.Opened || e.EventType == EmailEventType.Clicked)
                    .Max(e => (DateTime?)e.OccurredAt)))
            .ToListAsync(cancellationToken);
    }
}
