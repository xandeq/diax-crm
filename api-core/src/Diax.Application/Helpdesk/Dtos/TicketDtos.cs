using Diax.Domain.Helpdesk;

namespace Diax.Application.Helpdesk.Dtos;

public record CreateTicketRequest(
    string Subject,
    string? Description,
    TicketPriority Priority,
    TicketCategory Category,
    Guid? CustomerId,
    string? CustomerName);

public record UpdateTicketRequest(
    string Subject,
    string? Description,
    TicketStatus Status,
    TicketPriority Priority,
    TicketCategory Category,
    Guid? CustomerId,
    string? CustomerName);

public record TicketResponse(
    Guid Id,
    string Subject,
    string? Description,
    TicketStatus Status,
    TicketPriority Priority,
    TicketCategory Category,
    Guid? CustomerId,
    string? CustomerName,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record TicketsQuery(
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    TicketCategory? Category = null,
    Guid? CustomerId = null);
