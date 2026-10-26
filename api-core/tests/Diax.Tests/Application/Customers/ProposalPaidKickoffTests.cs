using Diax.Application.Customers;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

public class ProposalPaidKickoffTests
{
    private readonly Mock<IProposalRepository> _proposalRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Customer _customer = new("Padaria Teste", "contato@padaria.test");
    private readonly List<TaskItem> _tasks = new();

    public ProposalPaidKickoffTests()
    {
        _customerRepo.Setup(x => x.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
        _taskRepo.Setup(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => _tasks.Add(t));
    }

    private ProposalService Svc() => new(
        _proposalRepo.Object, _customerRepo.Object, Mock.Of<IServicePackageRepository>(), _taskRepo.Object, _uow.Object,
        Mock.Of<ITelegramSender>(), Mock.Of<IEmailDispatchService>(),
        Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance);

    private Proposal Proposta()
    {
        var p = new Proposal(_customer.Id, _owner, "Site Essencial", "Escopo", 1800m, null, null);
        _proposalRepo.Setup(x => x.GetByIdAsync(p.Id, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Fact]
    public async Task MarcarPaga_CriaTarefaDeInicioDoProjeto()
    {
        var p = Proposta();

        var r = await Svc().MarkPaidAsync(p.Id, _owner);

        Assert.True(r.IsSuccess);
        var t = Assert.Single(_tasks);
        Assert.Equal("Iniciar projeto: Padaria Teste", t.Title);
        Assert.Contains("Site Essencial", t.Description);
        Assert.Equal(TaskItemPriority.High, t.Priority);
        Assert.Equal(_owner, t.UserId);
        Assert.Equal(_customer.Id, t.CustomerId);
        Assert.Equal(CustomerStatus.Customer, _customer.Status);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarcarPagaDeNovo_NaoDuplicaTarefa()
    {
        var p = Proposta();
        await Svc().MarkPaidAsync(p.Id, _owner);
        await Svc().MarkPaidAsync(p.Id, _owner);

        Assert.Single(_tasks);
    }

    [Fact]
    public async Task PropostaDeOutroUsuario_NaoCriaTarefa()
    {
        var p = Proposta();
        var r = await Svc().MarkPaidAsync(p.Id, Guid.NewGuid());

        Assert.False(r.IsSuccess);
        Assert.Empty(_tasks);
    }
}
