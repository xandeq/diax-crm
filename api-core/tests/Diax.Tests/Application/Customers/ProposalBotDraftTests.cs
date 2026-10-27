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

public class ProposalBotDraftTests
{
    private readonly Mock<IProposalRepository> _proposalRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IServicePackageRepository> _packageRepo = new();
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Customer _customer = new("Padaria Teste", "contato@padaria.test");
    private Proposal? _captured;
    private TaskItem? _task;

    public ProposalBotDraftTests()
    {
        _customerRepo.Setup(x => x.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
        _proposalRepo.Setup(x => x.GetByCustomerAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Proposal>());
        _proposalRepo.Setup(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Callback<Proposal, CancellationToken>((p, _) => _captured = p);
        _taskRepo.Setup(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => _task = t);
    }

    private ProposalService Svc() => new(
        _proposalRepo.Object, _customerRepo.Object, _packageRepo.Object, _taskRepo.Object, _uow.Object,
        Mock.Of<ITelegramSender>(), Mock.Of<IEmailDispatchService>(),
        Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance);

    private ServicePackage Pacote(bool ativo = true, Guid? dono = null)
    {
        var p = new ServicePackage(dono ?? _owner, "Site Essencial", "Site de 1 página", "Página única", 1800m, 15, 0);
        if (!ativo) p.Update(p.Name, p.ShortDescription, p.Scope, p.Price, p.DeliveryDays, p.SortOrder, false);
        _packageRepo.Setup(x => x.GetByIdAsync(p.Id, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Fact]
    public async Task SemPropostaAberta_CriaRascunhoDoBotETarefaUrgente()
    {
        var pkg = Pacote();
        var resumo = new string('x', 300);

        var r = await Svc().CreateBotDraftAsync(_customer.Id, pkg.Id, resumo, _owner);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value.Created);
        Assert.NotNull(_captured);
        Assert.Equal(ProposalStatus.Draft, _captured!.Status);
        Assert.Equal(ProposalOrigin.Bot, _captured.Origin);
        Assert.Equal(pkg.Id, _captured.PackageId);
        Assert.Equal(1800m, _captured.Amount);
        Assert.Equal("Site Essencial", _captured.Title);
        Assert.Equal(CustomerStatus.Negotiating, _customer.Status);

        Assert.NotNull(_task);
        Assert.Equal("Revisar e enviar proposta: Padaria Teste", _task!.Title);
        Assert.Equal(TaskItemPriority.Urgent, _task.Priority);
        Assert.Equal(256, _task.Description!.Length);
        Assert.Equal(_owner, _task.UserId);
        Assert.Equal(_customer.Id, _task.CustomerId);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JaTemPropostaAberta_NaoCriaOutra()
    {
        var pkg = Pacote();
        var existente = new Proposal(_customer.Id, _owner, "Proposta antiga", "Escopo", 900m, null, null);
        _proposalRepo.Setup(x => x.GetByCustomerAsync(_customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Proposal> { existente });

        var r = await Svc().CreateBotDraftAsync(_customer.Id, pkg.Id, "quer site", _owner);

        Assert.True(r.IsSuccess);
        Assert.False(r.Value.Created);
        Assert.Equal(existente.Id, r.Value.Proposal.Id);
        _proposalRepo.Verify(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskRepo.Verify(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PropostaAnteriorVencida_NaoBloqueia_CriaRascunhoNovo()
    {
        var pkg = Pacote();
        var vencida = new Proposal(_customer.Id, _owner, "Proposta antiga", "Escopo", 900m, null, DateTime.UtcNow.AddDays(-1));
        vencida.MarkSent();
        _proposalRepo.Setup(x => x.GetByCustomerAsync(_customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Proposal> { vencida });

        var r = await Svc().CreateBotDraftAsync(_customer.Id, pkg.Id, "quer site", _owner);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value.Created);
        Assert.NotEqual(vencida.Id, r.Value.Proposal.Id);
        _proposalRepo.Verify(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PacoteInativoAlheioOuInexistente_FalhaENadaSalvo()
    {
        var ids = new[] { Pacote(ativo: false).Id, Pacote(dono: Guid.NewGuid()).Id, Guid.NewGuid() };
        foreach (var id in ids)
        {
            var r = await Svc().CreateBotDraftAsync(_customer.Id, id, null, _owner);
            Assert.False(r.IsSuccess);
        }
        _proposalRepo.Verify(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SemResumo_DescricaoDaTarefaCitaOPacote()
    {
        var pkg = Pacote();
        var r = await Svc().CreateBotDraftAsync(_customer.Id, pkg.Id, "  ", _owner);
        Assert.True(r.IsSuccess);
        Assert.Equal("Pacote Site Essencial", _task!.Description);
    }
}
