using Diax.Application.Customers;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

/// <summary>Renovar = estender a validade da mesma proposta: o link que o cliente já tem volta a valer.</summary>
public class ProposalRenewTests
{
    private readonly Mock<IProposalRepository> _proposalRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Customer _customer = new("Padaria Teste", "contato@padaria.test");

    public ProposalRenewTests()
    {
        _customerRepo.Setup(x => x.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
    }

    private ProposalService Svc() => new(
        _proposalRepo.Object, _customerRepo.Object, Mock.Of<IServicePackageRepository>(), Mock.Of<ITaskRepository>(), _uow.Object,
        Mock.Of<ITelegramSender>(), Mock.Of<IEmailDispatchService>(),
        Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance);

    private Proposal Vencida()
    {
        var p = new Proposal(_customer.Id, _owner, "Site Essencial", "Escopo", 1800m, "contato@padaria.test", DateTime.UtcNow.AddDays(-2));
        p.MarkSent();
        _proposalRepo.Setup(x => x.GetByIdAsync(p.Id, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Fact]
    public async Task Vencida_GanhaSeteDiasNoMesmoLink()
    {
        var p = Vencida();
        var token = p.PublicToken;

        var r = await Svc().RenewAsync(p.Id, _owner);

        Assert.True(r.IsSuccess);
        Assert.False(p.IsExpired);
        Assert.InRange(p.ValidUntil!.Value, DateTime.UtcNow.AddDays(6.9), DateTime.UtcNow.AddDays(7.1));
        Assert.Equal(token, p.PublicToken);
        Assert.Equal(ProposalStatus.Sent, p.Status);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Renovada_PodeAvisarDeNovoQuandoVencer()
    {
        var p = Vencida();
        p.RegisterFollowUpStage(3); // já tinha gerado a tarefa "Proposta vencida"

        await Svc().RenewAsync(p.Id, _owner);

        Assert.Equal(2, p.LastFollowUpStage);
        Assert.True(p.RegisterFollowUpStage(3)); // novo vencimento gera tarefa outra vez
    }

    [Fact]
    public async Task Renovada_VoltaAMostrarPixNoLinkPublico()
    {
        var p = Vencida();
        _proposalRepo.Setup(x => x.GetByPublicTokenAsync(p.PublicToken, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        Assert.Null((await Svc().GetPublicAsync(p.PublicToken)).Value.PixCopiaECola);

        await Svc().RenewAsync(p.Id, _owner);

        Assert.NotNull((await Svc().GetPublicAsync(p.PublicToken)).Value.PixCopiaECola);
    }

    [Fact]
    public async Task Paga_NaoRenova()
    {
        var p = Vencida();
        p.MarkPaid();

        var r = await Svc().RenewAsync(p.Id, _owner);

        Assert.False(r.IsSuccess);
        Assert.Contains("Validation", r.Error.Code);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelada_NaoRenova()
    {
        var p = Vencida();
        p.Cancel();

        var r = await Svc().RenewAsync(p.Id, _owner);

        Assert.False(r.IsSuccess);
        Assert.True(p.IsExpired);
    }

    [Fact]
    public async Task DeOutroUsuario_NotFound()
    {
        var p = Vencida();

        var r = await Svc().RenewAsync(p.Id, Guid.NewGuid());

        Assert.False(r.IsSuccess);
        Assert.Contains("NotFound", r.Error.Code);
        Assert.True(p.IsExpired);
    }
}
