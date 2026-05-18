using Diax.Domain.Helpdesk;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class TicketRepository : Repository<SupportTicket>, ITicketRepository
{
    private readonly DiaxDbContext _context;

    public TicketRepository(DiaxDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<SupportTicket?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

    public async Task<IEnumerable<SupportTicket>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(Guid userId, TicketStatus status, CancellationToken ct = default)
        => await _context.SupportTickets
            .Where(t => t.UserId == userId && t.Status == status)
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<SupportTicket>> GetByCustomerAsync(Guid userId, Guid customerId, CancellationToken ct = default)
        => await _context.SupportTickets
            .Where(t => t.UserId == userId && t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
}
