using Diax.Domain.Common;

namespace Diax.Domain.Helpdesk;

public class SupportTicket : AuditableEntity, IUserOwnedEntity
{
    public required string Subject { get; set; }
    public string? Description { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; } = TicketCategory.Other;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // IUserOwnedEntity
    public Guid UserId { get; set; }

    public void Resolve()
    {
        Status = TicketStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = TicketStatus.Open;
        ResolvedAt = null;
    }

    public void Close()
    {
        Status = TicketStatus.Closed;
        ResolvedAt ??= DateTime.UtcNow;
    }
}
