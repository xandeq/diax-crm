using Diax.Domain.Helpdesk;

namespace Diax.Tests.Domain.Helpdesk;

public class SupportTicketTests
{
    private static SupportTicket NewTicket(TicketStatus status = TicketStatus.Open) => new()
    {
        UserId = Guid.NewGuid(),
        Subject = "Test ticket",
        Status = status,
        Priority = TicketPriority.Medium,
        Category = TicketCategory.Question,
    };

    // ── Resolve ──────────────────────────────────────────────────────

    [Fact]
    public void Resolve_OpenTicket_SetsStatusResolved()
    {
        var ticket = NewTicket(TicketStatus.Open);
        ticket.Resolve();
        Assert.Equal(TicketStatus.Resolved, ticket.Status);
    }

    [Fact]
    public void Resolve_OpenTicket_SetsResolvedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var ticket = NewTicket(TicketStatus.Open);

        ticket.Resolve();

        Assert.NotNull(ticket.ResolvedAt);
        Assert.True(ticket.ResolvedAt >= before);
        Assert.True(ticket.ResolvedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Resolve_AlreadyResolvedTicket_OverwritesResolvedAt()
    {
        var ticket = NewTicket(TicketStatus.Resolved);
        var original = DateTime.UtcNow.AddDays(-1);
        ticket.ResolvedAt = original;

        ticket.Resolve();

        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.NotEqual(original, ticket.ResolvedAt);
    }

    [Fact]
    public void Resolve_InProgressTicket_SetsStatusResolved()
    {
        var ticket = NewTicket(TicketStatus.InProgress);
        ticket.Resolve();
        Assert.Equal(TicketStatus.Resolved, ticket.Status);
    }

    // ── Reopen ───────────────────────────────────────────────────────

    [Fact]
    public void Reopen_ResolvedTicket_SetsStatusOpen()
    {
        var ticket = NewTicket(TicketStatus.Resolved);
        ticket.ResolvedAt = DateTime.UtcNow;

        ticket.Reopen();

        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public void Reopen_ResolvedTicket_ClearsResolvedAt()
    {
        var ticket = NewTicket(TicketStatus.Resolved);
        ticket.ResolvedAt = DateTime.UtcNow;

        ticket.Reopen();

        Assert.Null(ticket.ResolvedAt);
    }

    [Fact]
    public void Reopen_ClosedTicket_SetsStatusOpen()
    {
        var ticket = NewTicket(TicketStatus.Closed);
        ticket.ResolvedAt = DateTime.UtcNow;

        ticket.Reopen();

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.ResolvedAt);
    }

    [Fact]
    public void Reopen_OpenTicket_RemainsOpen()
    {
        var ticket = NewTicket(TicketStatus.Open);
        ticket.Reopen();
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.ResolvedAt);
    }

    // ── Close ────────────────────────────────────────────────────────

    [Fact]
    public void Close_OpenTicket_SetsStatusClosed()
    {
        var ticket = NewTicket(TicketStatus.Open);
        ticket.Close();
        Assert.Equal(TicketStatus.Closed, ticket.Status);
    }

    [Fact]
    public void Close_OpenTicket_SetsResolvedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var ticket = NewTicket(TicketStatus.Open);

        ticket.Close();

        Assert.NotNull(ticket.ResolvedAt);
        Assert.True(ticket.ResolvedAt >= before);
    }

    [Fact]
    public void Close_ResolvedTicket_PreservesExistingResolvedAt()
    {
        var ticket = NewTicket(TicketStatus.Resolved);
        var original = DateTime.UtcNow.AddDays(-1);
        ticket.ResolvedAt = original;

        ticket.Close();

        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.Equal(original, ticket.ResolvedAt);
    }

    [Fact]
    public void Close_TicketWithNullResolvedAt_SetsResolvedAt()
    {
        var ticket = NewTicket(TicketStatus.InProgress);
        Assert.Null(ticket.ResolvedAt);

        ticket.Close();

        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.NotNull(ticket.ResolvedAt);
    }
}
