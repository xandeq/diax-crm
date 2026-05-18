using Diax.Domain.Common;

namespace Diax.Domain.Helpdesk;

public interface ITicketRepository : IRepository<SupportTicket>
{
    Task<SupportTicket?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<SupportTicket>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<SupportTicket>> GetByStatusAsync(Guid userId, TicketStatus status, CancellationToken ct = default);
    Task<IEnumerable<SupportTicket>> GetByCustomerAsync(Guid userId, Guid customerId, CancellationToken ct = default);
}
