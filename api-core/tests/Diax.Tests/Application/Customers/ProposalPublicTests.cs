using Diax.Application.Customers;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

/// <summary>Link público da proposta: visualização (aviso de "momento quente") e aceite pelo cliente.</summary>
public class ProposalPublicTests
{
    private readonly Mock<IProposalRepository> _proposalRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITelegramSender> _telegram = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Customer _customer = new("Padaria Teste", "contato@padaria.test");
    private readonly List<string> _avisos = new();

    public ProposalPublicTests()
    {
        _customerRepo.Setup(x => x.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
        _telegram.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((html, _) => _avisos.Add(html))
            .ReturnsAsync(true);
    }

    private ProposalService Svc() => new(
        _proposalRepo.Object, _customerRepo.Object, Mock.Of<IServicePackageRepository>(), Mock.Of<ITaskRepository>(), _uow.Object,
        _telegram.Object, Mock.Of<IEmailDispatchService>(),
        Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance);

    private Proposal Proposta(string? pixKey = "contato@padaria.test", DateTime? validUntil = null)
    {
        var p = new Proposal(_customer.Id, Guid.NewGuid(), "Site Essencial", "Escopo", 1800m, pixKey, validUntil);
        _proposalRepo.Setup(x => x.GetByPublicTokenAsync(p.PublicToken, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Theory]
    [InlineData("")]
    [InlineData("curto")]
    [InlineData("../../etc/passwd-aaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task TokenInvalido_NaoVaiAoBanco(string token)
    {
        var r = await Svc().GetPublicAsync(token);

        Assert.False(r.IsSuccess);
        _proposalRepo.Verify(x => x.GetByPublicTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TokenInexistente_NotFound()
    {
        var r = await Svc().GetPublicAsync(new string('a', 40));

        Assert.False(r.IsSuccess);
        Assert.Contains("NotFound", r.Error.Code);
    }

    [Fact]
    public async Task PrimeiraVisualizacao_AvisaUmaVezSo_EMarcaComoEnviada()
    {
        var p = Proposta();

        var r1 = await Svc().GetPublicAsync(p.PublicToken);
        await Svc().GetPublicAsync(p.PublicToken);

        Assert.True(r1.IsSuccess);
        Assert.Equal(ProposalStatus.Sent, p.Status);
        Assert.Equal(2, p.ViewCount);
        var aviso = Assert.Single(_avisos);
        Assert.Contains("visualizada", aviso);
        Assert.Contains("Padaria Teste", aviso);
    }

    [Fact]
    public async Task Visualizacao_DevolvePix_EEscondeDadosInternos()
    {
        var p = Proposta();

        var dto = (await Svc().GetPublicAsync(p.PublicToken)).Value;

        Assert.False(string.IsNullOrEmpty(dto.PixCopiaECola));
        Assert.StartsWith("000201", dto.PixCopiaECola);
        Assert.Equal("Padaria Teste", dto.CustomerName);
        Assert.Equal(1800m, dto.Amount);
    }

    [Fact]
    public async Task PropostaVencida_NaoGeraPix()
    {
        var p = Proposta(validUntil: DateTime.UtcNow.AddDays(-1));

        var dto = (await Svc().GetPublicAsync(p.PublicToken)).Value;

        Assert.True(dto.IsExpired);
        Assert.Null(dto.PixCopiaECola);
    }

    [Fact]
    public async Task Aceite_MudaStatus_EAvisaComContato()
    {
        var p = Proposta();

        var r = await Svc().AcceptPublicAsync(p.PublicToken);

        Assert.True(r.IsSuccess);
        Assert.Equal(ProposalStatus.Accepted, p.Status);
        Assert.NotNull(p.AcceptedAt);
        var aceite = Assert.Single(_avisos, a => a.Contains("PROPOSTA ACEITA"));
        Assert.Contains("contato@padaria.test", aceite);
    }

    [Fact]
    public async Task AceiteRepetido_NaoAvisaDeNovo()
    {
        var p = Proposta();
        await Svc().AcceptPublicAsync(p.PublicToken);
        var aceitoEm = p.AcceptedAt;

        var r = await Svc().AcceptPublicAsync(p.PublicToken);

        Assert.True(r.IsSuccess);
        Assert.Equal(aceitoEm, p.AcceptedAt);
        Assert.Single(_avisos, a => a.Contains("PROPOSTA ACEITA"));
    }

    [Fact]
    public async Task AceiteDePropostaVencida_Recusa()
    {
        var p = Proposta(validUntil: DateTime.UtcNow.AddDays(-1));

        var r = await Svc().AcceptPublicAsync(p.PublicToken);

        Assert.False(r.IsSuccess);
        Assert.Contains("Validation", r.Error.Code);
        Assert.NotEqual(ProposalStatus.Accepted, p.Status);
        Assert.DoesNotContain(_avisos, a => a.Contains("PROPOSTA ACEITA"));
    }

    [Fact]
    public async Task AceiteDePropostaCancelada_Recusa()
    {
        var p = Proposta();
        p.Cancel();

        var r = await Svc().AcceptPublicAsync(p.PublicToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(ProposalStatus.Cancelled, p.Status);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TelegramFora_NaoQuebraAceite()
    {
        _telegram.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("telegram fora"));
        var p = Proposta();

        var r = await Svc().AcceptPublicAsync(p.PublicToken);

        Assert.True(r.IsSuccess);
        Assert.Equal(ProposalStatus.Accepted, p.Status);
    }
}
