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
}
