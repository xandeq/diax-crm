using Diax.Application.Customers;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

/// <summary>Cancelar proposta registra o motivo da perda (opcional) para aprender por que não fecha.</summary>
public class ProposalLossReasonTests
{
    private readonly Mock<IProposalRepository> _proposalRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Customer _customer = new("Padaria Teste", "contato@padaria.test");

    public ProposalLossReasonTests()
    {
        _customerRepo.Setup(x => x.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
    }

    private ProposalService Svc() => new(
        _proposalRepo.Object, _customerRepo.Object, Mock.Of<IServicePackageRepository>(), Mock.Of<ITaskRepository>(), _uow.Object,
        Mock.Of<ITelegramSender>(), Mock.Of<IEmailDispatchService>(),
        Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance);

    private Proposal Enviada()
    {
        var p = new Proposal(_customer.Id, _owner, "Site Essencial", "Escopo", 1800m, null, DateTime.UtcNow.AddDays(5));
        p.MarkSent();
        _proposalRepo.Setup(x => x.GetByIdAsync(p.Id, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Fact]
    public async Task CancelarComMotivo_GravaMotivoNotaEData()
    {
        var p = Enviada();

        var r = await Svc().CancelAsync(p.Id, _owner, ProposalLossReason.Price, "  achou caro  ");

        Assert.True(r.IsSuccess);
        Assert.Equal(ProposalStatus.Cancelled, p.Status);
        Assert.Equal(ProposalLossReason.Price, p.LossReason);
        Assert.Equal("achou caro", p.LossNote);
        Assert.NotNull(p.CancelledAt);
        Assert.Equal(ProposalLossReason.Price, r.Value.LossReason);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarSemMotivo_ContinuaFuncionando()
    {
        var p = Enviada();

        var r = await Svc().CancelAsync(p.Id, _owner);

        Assert.True(r.IsSuccess);
        Assert.Equal(ProposalStatus.Cancelled, p.Status);
        Assert.Null(p.LossReason);
        Assert.Null(p.LossNote);
    }

    [Fact]
    public void CancelarDeNovoSemMotivo_NaoApagaMotivoNemData()
    {
        var p = Enviada();
        p.Cancel(ProposalLossReason.NoResponse, "sumiu");
        var quando = p.CancelledAt;

        p.Cancel();

        Assert.Equal(ProposalLossReason.NoResponse, p.LossReason);
        Assert.Equal("sumiu", p.LossNote);
        Assert.Equal(quando, p.CancelledAt);
    }

    [Fact]
    public void NotaGrande_ETruncadaEm500()
    {
        var p = Enviada();
        p.Cancel(ProposalLossReason.Other, new string('x', 900));
        Assert.Equal(500, p.LossNote!.Length);
    }

    [Fact]
    public async Task MotivoInvalido_RetornaValidacaoENaoCancela()
    {
        var p = Enviada();

        var r = await Svc().CancelAsync(p.Id, _owner, (ProposalLossReason)99, null);

        Assert.True(r.IsFailure);
        Assert.Contains("Validation", r.Error.Code, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ProposalStatus.Sent, p.Status);
        Assert.Null(p.LossReason);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PropostaPaga_NaoCancelaENaoGravaMotivo()
    {
        var p = Enviada();
        p.MarkPaid();

        var r = await Svc().CancelAsync(p.Id, _owner, ProposalLossReason.Price, null);

        Assert.True(r.IsFailure);
        Assert.Null(p.LossReason);
    }
}
