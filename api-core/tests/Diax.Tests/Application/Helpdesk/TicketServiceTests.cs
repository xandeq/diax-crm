using Diax.Application.Helpdesk;
using Diax.Application.Helpdesk.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Helpdesk;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Helpdesk;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private TicketService Build() => new(
        _repo.Object,
        _uow.Object,
        NullLogger<TicketService>.Instance);

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static CreateTicketRequest ValidCreateRequest() => new(
        Subject: "Problema no login",
        Description: "Não consigo acessar o sistema",
        Priority: TicketPriority.High,
        Category: TicketCategory.Bug,
        CustomerId: null,
        CustomerName: null);

    private static SupportTicket NewTicket(Guid? userId = null) => new()
    {
        Subject = "Problema no login",
        Description = "Não consigo acessar o sistema",
        Priority = TicketPriority.High,
        Category = TicketCategory.Bug,
        UserId = userId ?? UserId,
    };

    // ── CreateAsync — happy path ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsTicketResponse()
    {
        var req = ValidCreateRequest();

        _repo.Setup(r => r.AddAsync(It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket t, CancellationToken _) => t);

        var result = await Build().CreateAsync(req, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Problema no login", result.Value.Subject);
        Assert.Equal(TicketPriority.High, result.Value.Priority);
        Assert.Equal(TicketCategory.Bug, result.Value.Category);
        Assert.Equal(TicketStatus.Open, result.Value.Status);
        _repo.Verify(r => r.AddAsync(It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CreateAsync — Subject required ───────────────────────────────

    [Fact]
    public async Task CreateAsync_EmptySubject_ReturnsFailure()
    {
        var req = new CreateTicketRequest(
            Subject: "",
            Description: "Descrição qualquer",
            Priority: TicketPriority.Medium,
            Category: TicketCategory.Other,
            CustomerId: null,
            CustomerName: null);

        var result = await Build().CreateAsync(req, UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.SubjectRequired", result.Error.Code);
        _repo.Verify(r => r.AddAsync(It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhitespaceSubject_ReturnsFailure()
    {
        var req = new CreateTicketRequest(
            Subject: "   ",
            Description: null,
            Priority: TicketPriority.Low,
            Category: TicketCategory.Other,
            CustomerId: null,
            CustomerName: null);

        var result = await Build().CreateAsync(req, UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.SubjectRequired", result.Error.Code);
    }

    // ── CreateAsync — Subject max length (512) ───────────────────────
    // NOTE: The service does not currently enforce a max-length check beyond
    // database constraints. This test documents that a 512-char subject is
    // accepted without validation failure at the service layer.

    [Fact]
    public async Task CreateAsync_SubjectAtMaxLength_Succeeds()
    {
        var longSubject = new string('A', 512);
        var req = new CreateTicketRequest(
            Subject: longSubject,
            Description: null,
            Priority: TicketPriority.Low,
            Category: TicketCategory.Other,
            CustomerId: null,
            CustomerName: null);

        _repo.Setup(r => r.AddAsync(It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket t, CancellationToken _) => t);

        var result = await Build().CreateAsync(req, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(longSubject, result.Value.Subject);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsMappedListForUser()
    {
        var tickets = new List<SupportTicket>
        {
            NewTicket(),
            NewTicket(),
        };

        _repo.Setup(r => r.GetByUserAsync(UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(tickets);

        var result = await Build().GetAllAsync(UserId, new TicketsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetAllAsync_FilterByStatus_CallsGetByStatusAsync()
    {
        var tickets = new List<SupportTicket> { NewTicket() };

        _repo.Setup(r => r.GetByStatusAsync(UserId, TicketStatus.Open, It.IsAny<CancellationToken>()))
             .ReturnsAsync(tickets);

        var result = await Build().GetAllAsync(UserId, new TicketsQuery(Status: TicketStatus.Open));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        _repo.Verify(r => r.GetByStatusAsync(UserId, TicketStatus.Open, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_FilterByCustomer_CallsGetByCustomerAsync()
    {
        var customerId = Guid.NewGuid();
        var tickets = new List<SupportTicket> { NewTicket() };

        _repo.Setup(r => r.GetByCustomerAsync(UserId, customerId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(tickets);

        var result = await Build().GetAllAsync(UserId, new TicketsQuery(CustomerId: customerId));

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.GetByCustomerAsync(UserId, customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_FilterByPriority_FiltersInMemory()
    {
        var highTicket = NewTicket();
        highTicket.Priority = TicketPriority.High;

        var lowTicket = NewTicket();
        lowTicket.Priority = TicketPriority.Low;

        _repo.Setup(r => r.GetByUserAsync(UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<SupportTicket> { highTicket, lowTicket });

        var result = await Build().GetAllAsync(UserId, new TicketsQuery(Priority: TicketPriority.High));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(TicketPriority.High, result.Value.First().Priority);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_TicketExists_ReturnsTicketResponse()
    {
        var ticket = NewTicket();
        var ticketId = ticket.Id;

        _repo.Setup(r => r.GetByIdAndUserAsync(ticketId, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().GetByIdAsync(ticketId, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Problema no login", result.Value.Subject);
    }

    [Fact]
    public async Task GetByIdAsync_TicketNotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket?)null);

        var result = await Build().GetByIdAsync(Guid.NewGuid(), UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.NotFound", result.Error.Code);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFieldsAndSaves()
    {
        var ticket = NewTicket();
        var ticketId = ticket.Id;
        var updateReq = new UpdateTicketRequest(
            Subject: "Assunto atualizado",
            Description: "Nova descrição",
            Status: TicketStatus.InProgress,
            Priority: TicketPriority.Urgent,
            Category: TicketCategory.Billing,
            CustomerId: null,
            CustomerName: null);

        _repo.Setup(r => r.GetByIdAndUserAsync(ticketId, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().UpdateAsync(ticketId, updateReq, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Assunto atualizado", result.Value.Subject);
        Assert.Equal(TicketStatus.InProgress, result.Value.Status);
        Assert.Equal(TicketPriority.Urgent, result.Value.Priority);
        Assert.Null(result.Value.ResolvedAt);
        _repo.Verify(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket?)null);

        var updateReq = new UpdateTicketRequest(
            Subject: "X", Description: null,
            Status: TicketStatus.Open,
            Priority: TicketPriority.Medium,
            Category: TicketCategory.Other,
            CustomerId: null, CustomerName: null);

        var result = await Build().UpdateAsync(Guid.NewGuid(), updateReq, UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_EmptySubject_ReturnsFailure()
    {
        var ticket = NewTicket();

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var updateReq = new UpdateTicketRequest(
            Subject: "   ", Description: null,
            Status: TicketStatus.Open,
            Priority: TicketPriority.Medium,
            Category: TicketCategory.Other,
            CustomerId: null, CustomerName: null);

        var result = await Build().UpdateAsync(ticket.Id, updateReq, UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.SubjectRequired", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_StatusResolvedSetsResolvedAt()
    {
        var ticket = NewTicket();

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var updateReq = new UpdateTicketRequest(
            Subject: "Resolvido", Description: null,
            Status: TicketStatus.Resolved,
            Priority: TicketPriority.Medium,
            Category: TicketCategory.Other,
            CustomerId: null, CustomerName: null);

        var before = DateTime.UtcNow;
        var result = await Build().UpdateAsync(ticket.Id, updateReq, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Resolved, result.Value.Status);
        Assert.NotNull(result.Value.ResolvedAt);
        Assert.True(result.Value.ResolvedAt >= before);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_TicketExists_DeletesAndSaves()
    {
        var ticket = NewTicket();

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().DeleteAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.DeleteAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_TicketNotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket?)null);

        var result = await Build().DeleteAsync(Guid.NewGuid(), UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.NotFound", result.Error.Code);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── ResolveAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_OpenTicket_SetsStatusResolvedAndResolvedAt()
    {
        var ticket = NewTicket();
        Assert.Equal(TicketStatus.Open, ticket.Status);

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var before = DateTime.UtcNow;
        var result = await Build().ResolveAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Resolved, result.Value.Status);
        Assert.NotNull(result.Value.ResolvedAt);
        Assert.True(result.Value.ResolvedAt >= before);
        _repo.Verify(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_AlreadyResolvedTicket_OverwritesResolvedAt()
    {
        // SupportTicket.Resolve() does not guard against double-resolve;
        // calling it again simply overwrites Status and ResolvedAt.
        var ticket = NewTicket();
        var originalResolvedAt = DateTime.UtcNow.AddHours(-1);
        ticket.Resolve();
        ticket.ResolvedAt = originalResolvedAt;

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().ResolveAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Resolved, result.Value.Status);
        // ResolvedAt is refreshed by the second Resolve() call
        Assert.True(result.Value.ResolvedAt >= originalResolvedAt);
    }

    [Fact]
    public async Task ResolveAsync_NotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket?)null);

        var result = await Build().ResolveAsync(Guid.NewGuid(), UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.NotFound", result.Error.Code);
    }

    // ── ReopenAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ReopenAsync_ResolvedTicket_SetsStatusOpenAndClearsResolvedAt()
    {
        var ticket = NewTicket();
        ticket.Resolve();
        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.NotNull(ticket.ResolvedAt);

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().ReopenAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Open, result.Value.Status);
        Assert.Null(result.Value.ResolvedAt);
        _repo.Verify(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReopenAsync_ClosedTicket_SetsStatusOpenAndClearsResolvedAt()
    {
        var ticket = NewTicket();
        ticket.Close();
        Assert.Equal(TicketStatus.Closed, ticket.Status);

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().ReopenAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Open, result.Value.Status);
        Assert.Null(result.Value.ResolvedAt);
    }

    [Fact]
    public async Task ReopenAsync_NotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket?)null);

        var result = await Build().ReopenAsync(Guid.NewGuid(), UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.NotFound", result.Error.Code);
    }

    // ── CloseAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_OpenTicket_SetsStatusClosedAndResolvedAt()
    {
        var ticket = NewTicket();

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var before = DateTime.UtcNow;
        var result = await Build().CloseAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Closed, result.Value.Status);
        Assert.NotNull(result.Value.ResolvedAt);
        Assert.True(result.Value.ResolvedAt >= before);
        _repo.Verify(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_AlreadyResolvedTicket_PreservesExistingResolvedAt()
    {
        // SupportTicket.Close() uses ??= so it preserves ResolvedAt if already set.
        var ticket = NewTicket();
        var originalResolvedAt = DateTime.UtcNow.AddHours(-2);
        ticket.Resolve();
        ticket.ResolvedAt = originalResolvedAt;

        _repo.Setup(r => r.GetByIdAndUserAsync(ticket.Id, UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ticket);

        var result = await Build().CloseAsync(ticket.Id, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Closed, result.Value.Status);
        Assert.Equal(originalResolvedAt, result.Value.ResolvedAt);
    }

    [Fact]
    public async Task CloseAsync_NotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((SupportTicket?)null);

        var result = await Build().CloseAsync(Guid.NewGuid(), UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Ticket.NotFound", result.Error.Code);
    }
}
