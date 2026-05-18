using Diax.Application.Helpdesk.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Helpdesk;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Helpdesk;

public class TicketService(
    ITicketRepository ticketRepo,
    IUnitOfWork unitOfWork,
    ILogger<TicketService> logger) : ITicketService
{
    public async Task<Result<IEnumerable<TicketResponse>>> GetAllAsync(Guid userId, TicketsQuery query, CancellationToken ct = default)
    {
        IEnumerable<SupportTicket> tickets;

        if (query.Status.HasValue)
            tickets = await ticketRepo.GetByStatusAsync(userId, query.Status.Value, ct);
        else if (query.CustomerId.HasValue)
            tickets = await ticketRepo.GetByCustomerAsync(userId, query.CustomerId.Value, ct);
        else
            tickets = await ticketRepo.GetByUserAsync(userId, ct);

        if (query.Priority.HasValue)
            tickets = tickets.Where(t => t.Priority == query.Priority.Value);

        if (query.Category.HasValue)
            tickets = tickets.Where(t => t.Category == query.Category.Value);

        return Result.Success(tickets.Select(MapToResponse));
    }

    public async Task<Result<TicketResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.GetByIdAndUserAsync(id, userId, ct);
        if (ticket is null)
            return Result.Failure<TicketResponse>(new Error("Ticket.NotFound", "Ticket não encontrado."));

        return Result.Success(MapToResponse(ticket));
    }

    public async Task<Result<TicketResponse>> CreateAsync(CreateTicketRequest request, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
            return Result.Failure<TicketResponse>(new Error("Ticket.SubjectRequired", "O assunto é obrigatório."));

        var ticket = new SupportTicket
        {
            Subject = request.Subject.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            Category = request.Category,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName?.Trim(),
            UserId = userId,
        };

        await ticketRepo.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {TicketId} created for user {UserId}", ticket.Id, userId);
        return Result.Success(MapToResponse(ticket));
    }

    public async Task<Result<TicketResponse>> UpdateAsync(Guid id, UpdateTicketRequest request, Guid userId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.GetByIdAndUserAsync(id, userId, ct);
        if (ticket is null)
            return Result.Failure<TicketResponse>(new Error("Ticket.NotFound", "Ticket não encontrado."));

        if (string.IsNullOrWhiteSpace(request.Subject))
            return Result.Failure<TicketResponse>(new Error("Ticket.SubjectRequired", "O assunto é obrigatório."));

        ticket.Subject = request.Subject.Trim();
        ticket.Description = request.Description?.Trim();
        ticket.Status = request.Status;
        ticket.Priority = request.Priority;
        ticket.Category = request.Category;
        ticket.CustomerId = request.CustomerId;
        ticket.CustomerName = request.CustomerName?.Trim();

        if (request.Status == TicketStatus.Resolved && ticket.ResolvedAt is null)
            ticket.ResolvedAt = DateTime.UtcNow;
        else if (request.Status == TicketStatus.Closed && ticket.ResolvedAt is null)
            ticket.ResolvedAt = DateTime.UtcNow;
        else if (request.Status == TicketStatus.Open || request.Status == TicketStatus.InProgress)
            ticket.ResolvedAt = null;

        await ticketRepo.UpdateAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(ticket));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.GetByIdAndUserAsync(id, userId, ct);
        if (ticket is null)
            return Result.Failure(new Error("Ticket.NotFound", "Ticket não encontrado."));

        await ticketRepo.DeleteAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {TicketId} deleted for user {UserId}", id, userId);
        return Result.Success();
    }

    public async Task<Result<TicketResponse>> ResolveAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.GetByIdAndUserAsync(id, userId, ct);
        if (ticket is null)
            return Result.Failure<TicketResponse>(new Error("Ticket.NotFound", "Ticket não encontrado."));

        ticket.Resolve();
        await ticketRepo.UpdateAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(ticket));
    }

    public async Task<Result<TicketResponse>> ReopenAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.GetByIdAndUserAsync(id, userId, ct);
        if (ticket is null)
            return Result.Failure<TicketResponse>(new Error("Ticket.NotFound", "Ticket não encontrado."));

        ticket.Reopen();
        await ticketRepo.UpdateAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(ticket));
    }

    public async Task<Result<TicketResponse>> CloseAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.GetByIdAndUserAsync(id, userId, ct);
        if (ticket is null)
            return Result.Failure<TicketResponse>(new Error("Ticket.NotFound", "Ticket não encontrado."));

        ticket.Close();
        await ticketRepo.UpdateAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(ticket));
    }

    private static TicketResponse MapToResponse(SupportTicket t) => new(
        t.Id, t.Subject, t.Description, t.Status, t.Priority, t.Category,
        t.CustomerId, t.CustomerName, t.ResolvedAt, t.CreatedAt, t.UpdatedAt);
}
