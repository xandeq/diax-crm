using Diax.Application.Customers;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

public class ProposalFromPackageTests
{
    private readonly Mock<IProposalRepository> _proposalRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IServicePackageRepository> _packageRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Customer _customer = new("Padaria Teste", "contato@padaria.test");
    private Proposal? _captured;

    public ProposalFromPackageTests()
    {
        _customerRepo.Setup(x => x.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
        _proposalRepo.Setup(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Callback<Proposal, CancellationToken>((p, _) => _captured = p);
    }

    private ProposalService Svc() => new(
        _proposalRepo.Object, _customerRepo.Object, _packageRepo.Object, Mock.Of<ITaskRepository>(), _uow.Object,
        Mock.Of<ITelegramSender>(), Mock.Of<IEmailDispatchService>(),
        Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance);

    private ServicePackage Pacote(Guid? dono = null)
    {
        var p = new ServicePackage(dono ?? _user, "Site Essencial", "Site de 1 página", "Página única\nBotão WhatsApp", 1800m, 15, 0);
        _packageRepo.Setup(x => x.GetByIdAsync(p.Id, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Fact]
    public async Task PacoteComCamposVazios_UsaNomeDescricaoEPrecoDoPacote()
    {
        var pkg = Pacote();
        var r = await Svc().CreateAsync(new CreateProposalRequest(_customer.Id, "", "", 0m, null, null, pkg.Id), _user);

        Assert.True(r.IsSuccess);
        Assert.NotNull(_captured);
        Assert.Equal("Site Essencial", _captured!.Title);
        Assert.Equal(pkg.ToProposalDescription(), _captured.Description);
        Assert.Equal(1800m, _captured.Amount);
        Assert.Equal(pkg.Id, _captured.PackageId);
        Assert.Equal(ProposalOrigin.Manual, _captured.Origin);
        Assert.Equal(pkg.Id, r.Value.PackageId);
        Assert.Equal(1800m, _customer.EstimatedValue);
    }

    [Fact]
    public async Task PacoteComValorInformado_ValorDoRequestPrevalece()
    {
        var pkg = Pacote();
        var r = await Svc().CreateAsync(new CreateProposalRequest(_customer.Id, "Site com desconto", "Escopo ajustado", 1500m, null, null, pkg.Id), _user);

        Assert.True(r.IsSuccess);
        Assert.Equal("Site com desconto", _captured!.Title);
        Assert.Equal("Escopo ajustado", _captured.Description);
        Assert.Equal(1500m, _captured.Amount);
        Assert.Equal(pkg.Id, _captured.PackageId);
    }

    [Fact]
    public async Task PacoteDeOutroUsuarioOuInexistente_NotFoundENadaSalvo()
    {
        var alheio = Pacote(dono: Guid.NewGuid());
        foreach (var id in new[] { alheio.Id, Guid.NewGuid() })
        {
            var r = await Svc().CreateAsync(new CreateProposalRequest(_customer.Id, "", "", 0m, null, null, id), _user);
            Assert.False(r.IsSuccess);
        }
        _proposalRepo.Verify(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SemPacote_ComportamentoAtual()
    {
        var r = await Svc().CreateAsync(new CreateProposalRequest(_customer.Id, "Landing", "Escopo", 900m, null, null), _user);

        Assert.True(r.IsSuccess);
        Assert.Null(_captured!.PackageId);
        Assert.Equal(ProposalOrigin.Manual, _captured.Origin);
        Assert.Equal(900m, _captured.Amount);
        _packageRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
