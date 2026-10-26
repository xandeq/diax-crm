using Diax.Application.Customers;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

public class ProposalFollowUpTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact] public void Draft_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Draft, Now.AddDays(-5), 0, false, Now));

    [Fact] public void SentRecent_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddHours(-47), 0, false, Now));

    [Fact] public void Sent48hNotViewed_Stage1() =>
        Assert.Equal(1, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddHours(-49), 0, false, Now));

    [Fact] public void Sent48hViewed_Stage2() =>
        Assert.Equal(2, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddHours(-49), 3, false, Now));

    [Fact] public void Expired_Stage3() =>
        Assert.Equal(3, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddDays(-9), 1, true, Now));

    [Fact] public void Accepted_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Accepted, Now.AddDays(-9), 1, true, Now));

    [Fact] public void Accepted48hUnpaid_Stage4() =>
        Assert.Equal(4, ProposalFollowUpService.StageFor(ProposalStatus.Accepted, Now.AddDays(-5), 2, false, Now, Now.AddHours(-49)));

    [Fact] public void AcceptedRecent_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Accepted, Now.AddDays(-5), 2, false, Now, Now.AddHours(-47)));

    [Fact] public void Paid_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Paid, Now.AddDays(-5), 2, false, Now, Now.AddDays(-4)));

    [Fact] public void SentWithoutSentAt_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Sent, null, 0, false, Now));

    [Theory]
    [InlineData(1, "Proposta não aberta")]
    [InlineData(2, "Proposta vista sem aceite")]
    [InlineData(3, "Proposta vencida")]
    [InlineData(4, "Proposta aceita sem pagamento")]
    public void TaskTitle_PerStage(int stage, string prefix) =>
        Assert.StartsWith(prefix, ProposalFollowUpService.TaskTitle(stage, "Padaria X"));

    [Fact]
    public async Task RunAsync_SameStageTwice_CreatesOneTask()
    {
        var owner = Guid.NewGuid();
        var customer = new Customer("Padaria X", "contato@padaria.test");
        var proposal = new Proposal(customer.Id, owner, "Site Essencial", "Escopo", 1800m, null, DateTime.UtcNow.AddDays(30));
        proposal.MarkSent();

        var proposals = new Mock<IProposalRepository>();
        proposals.Setup(x => x.GetAwaitingClientAsync(owner, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Proposal> { proposal });
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        var tasks = new Mock<ITaskRepository>();
        TaskItem? created = null;
        tasks.Setup(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => created = t);
        var uow = new Mock<IUnitOfWork>();
        var svc = new ProposalFollowUpService(proposals.Object, customers.Object, tasks.Object, uow.Object,
            NullLogger<ProposalFollowUpService>.Instance);

        var later = DateTime.UtcNow.AddDays(3);
        Assert.Equal(1, await svc.RunAsync(owner, later));
        Assert.Equal(0, await svc.RunAsync(owner, later));

        tasks.Verify(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Proposta não aberta: Padaria X", created!.Title);
        Assert.Equal(owner, created.UserId);
        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(TaskItemPriority.High, created.Priority);
        Assert.Equal(1, proposal.LastFollowUpStage);
    }
}
