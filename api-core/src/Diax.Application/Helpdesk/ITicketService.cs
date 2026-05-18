using Diax.Application.Helpdesk.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Helpdesk;

public interface ITicketService
{
    Task<Result<IEnumerable<TicketResponse>>> GetAllAsync(Guid userId, TicketsQuery query, CancellationToken ct = default);
    Task<Result<TicketResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TicketResponse>> CreateAsync(CreateTicketRequest request, Guid userId, CancellationToken ct = default);
    Task<Result<TicketResponse>> UpdateAsync(Guid id, UpdateTicketRequest request, Guid userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TicketResponse>> ResolveAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TicketResponse>> ReopenAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TicketResponse>> CloseAsync(Guid id, Guid userId, CancellationToken ct = default);
}
