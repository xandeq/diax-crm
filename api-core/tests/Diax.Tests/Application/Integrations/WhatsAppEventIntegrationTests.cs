using System.Linq.Expressions;
using Diax.Api.Controllers.V1;
using Diax.Application.Briefings;
using Diax.Application.Customers;
using Diax.Application.Notifications;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Integrations;
using Diax.Application.Integrations.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Integrations;

/// <summary>
/// POST /api/v1/integrations/whatsapp-event — callback do sender externo (n8n + WAHA)
/// fechando o loop de outreach: o CRM é a fonte de verdade, então quem envia reporta
/// de volta o que aconteceu com cada mensagem.
/// </summary>
public class WhatsAppEventIntegrationTests
{
    private readonly Mock<ICashFlowProjectionIntegrationService> _cashFlowService = new();
    private readonly Mock<IDailyBriefingService> _dailyBriefingService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEmailDispatchService> _emailDispatch = new();
    private readonly Mock<IProviderQuotaGuard> _quotaGuard = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IServicePackageRepository> _packageRepository = new();
    private readonly Mock<ILogger<IntegrationsController>> _logger = new();

    private const string ValidKey = "test-whatsapp-key";
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    private IntegrationsController BuildController(bool configured = true)
    {
        var data = new Dictionary<string, string?>
        {
            ["Integrations:DefaultUserId"] = OwnerUserId.ToString()
        };
        if (configured)
            data["Integrations:WhatsAppEventKey"] = ValidKey;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        return new IntegrationsController(
            _cashFlowService.Object,
            _dailyBriefingService.Object,
            _userRepository.Object,
            _emailDispatch.Object,
            _quotaGuard.Object,
            _customerRepository.Object,
            _taskRepository.Object,
            _unitOfWork.Object,
            new ProposalService(_proposalRepository.Object, _customerRepository.Object, _packageRepository.Object,
                _taskRepository.Object, _unitOfWork.Object, Mock.Of<ITelegramSender>(), _emailDispatch.Object,
                Mock.Of<IProposalCoverImageGenerator>(), NullLogger<ProposalService>.Instance),
            new ServicePackageService(_packageRepository.Object, _unitOfWork.Object),
            configuration,
            _logger.Object);
    }

    private static WhatsAppEventIntegrationRequest Request(
        Guid? customerId = null,
        string? phone = null,
        string @event = "sent",
        string? text = null) => new()
    {
        CustomerId = customerId,
        Phone = phone,
        Event = @event,
        Text = text,
        Provider = "waha",
    };

    [Fact]
    public async Task WrongKey_ReturnsUnauthorized()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent("wrong-key", Request(customerId: customer.Id), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task NotConfigured_ReturnsServiceUnavailable()
    {
        var sut = BuildController(configured: false);

        var result = await sut.WhatsAppEvent(ValidKey, Request(customerId: Guid.NewGuid()), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }

    [Fact]
    public async Task Sent_IncrementsWhatsAppSentCount_AndMovesToContacted()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent(ValidKey, Request(customerId: customer.Id, @event: "sent"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, customer.WhatsAppSentCount);
        Assert.Equal(CustomerStatus.Contacted, customer.Status);
        _customerRepository.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reply_AdvancesStatusToQualified_AndCreatesUrgentTask()
    {
        var sut = BuildController();
        var customer = new Customer("Maria", "maria@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent(
            ValidKey, Request(customerId: customer.Id, @event: "reply", text: "Quero saber mais!"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(CustomerStatus.Qualified, customer.Status);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t =>
                t.UserId == OwnerUserId &&
                t.CustomerId == customer.Id &&
                t.Priority == TaskItemPriority.Urgent &&
                t.Title.Contains("Maria")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Optout_SetsWhatsAppOptOutTrue()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent(ValidKey, Request(customerId: customer.Id, @event: "optout"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(customer.WhatsAppOptOut);
    }

    [Fact]
    public async Task Failed_DoesNotChangeCustomer_ReturnsOk()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent(ValidKey, Request(customerId: customer.Id, @event: "failed"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(CustomerStatus.Lead, customer.Status);
        _customerRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PhoneLookup_MatchesMaskedPhoneAgainstDigitsOnlyInput()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        customer.UpdateContactInfo(phone: "(27) 99999-0000");
        _customerRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([customer]);

        var result = await sut.WhatsAppEvent(
            ValidKey, Request(phone: "5527999990000", @event: "sent"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, customer.WhatsAppSentCount);
    }

    [Fact]
    public async Task PhoneLookup_MesmoTelefoneEmDoisLeads_ReplyVaiParaQuemRecebeuWhatsApp()
    {
        var sut = BuildController();
        // Ordem da lista do banco não define o dono: o lead duplicado vem primeiro.
        var duplicado = new Customer("Lead duplicado (import)", "dup@example.com");
        duplicado.UpdateContactInfo(phone: "(27) 99999-0000");
        var contatado = new Customer("Lead contatado", "lead@example.com");
        contatado.UpdateContactInfo(whatsApp: "5527999990000");
        contatado.RegisterWhatsAppSent();
        _customerRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([duplicado, contatado]);

        var result = await sut.WhatsAppEvent(
            ValidKey, Request(phone: "5527999990000@c.us", @event: "reply", text: "tenho interesse"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.CustomerId == contatado.Id), It.IsAny<CancellationToken>()), Times.Once);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.CustomerId == duplicado.Id), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void EscolherPorTelefone_PrefereQuemRecebeuWhatsAppMaisRecente_DepoisCampoWhatsApp()
    {
        var soPhone = new Customer("A", "a@example.com");
        soPhone.UpdateContactInfo(phone: "27 99999-0000");
        var campoWa = new Customer("B", "b@example.com");
        campoWa.UpdateContactInfo(whatsApp: "27999990000");

        // Ninguém recebeu WhatsApp: o match no campo WhatsApp ganha do match no Phone.
        Assert.Same(campoWa, IntegrationsController.PickByPhone([soPhone, campoWa], "5527999990000"));

        soPhone.RegisterWhatsAppSent();
        // Quem recebeu o WhatsApp ganha de tudo.
        Assert.Same(soPhone, IntegrationsController.PickByPhone([campoWa, soPhone], "5527999990000"));

        Assert.Null(IntegrationsController.PickByPhone([soPhone], "5521888887777"));
    }

    [Fact]
    public void EscolherPorTelefone_SemWhatsAppEnviado_NaoDependeDaOrdemDoBanco()
    {
        // Antes o último critério era a posição na lista do FindAsync (sem ORDER BY): a mesma
        // resposta podia cair em leads diferentes conforme o plano de execução do SQL.
        var frio = new Customer("Frio", "frio@example.com");
        frio.UpdateContactInfo(phone: "27 99999-0000");
        var contatado = new Customer("Contatado", "contatado@example.com");
        contatado.UpdateContactInfo(phone: "(27) 99999-0000");
        contatado.RegisterContact(); // contato registrado por outro canal (email/ligação)

        Assert.Same(contatado, IntegrationsController.PickByPhone([frio, contatado], "5527999990000"));
        Assert.Same(contatado, IntegrationsController.PickByPhone([contatado, frio], "5527999990000"));

        // Sem contato em nenhum: o que está mais avançado no funil.
        var lead = new Customer("Lead", "lead@example.com");
        lead.UpdateContactInfo(phone: "27999990000");
        var negociando = new Customer("Negociando", "neg@example.com");
        negociando.UpdateContactInfo(phone: "27999990000");
        negociando.UpdateStatus(Diax.Domain.Customers.Enums.CustomerStatus.Negotiating);

        Assert.Same(negociando, IntegrationsController.PickByPhone([lead, negociando], "5527999990000"));
        Assert.Same(negociando, IntegrationsController.PickByPhone([negociando, lead], "5527999990000"));

        // Inativo não ganha de lead ativo (o enum Inactive=5 vale mais que Negotiating=3).
        var inativo = new Customer("Inativo", "inativo@example.com");
        inativo.UpdateContactInfo(phone: "27999990000");
        inativo.UpdateStatus(Diax.Domain.Customers.Enums.CustomerStatus.Inactive);
        Assert.Same(negociando, IntegrationsController.PickByPhone([inativo, negociando], "5527999990000"));

        // Empate total: ainda assim o mesmo lead nas duas ordens.
        var a = new Customer("A", "a2@example.com"); a.UpdateContactInfo(phone: "27999990000");
        var b = new Customer("B", "b2@example.com"); b.UpdateContactInfo(phone: "27999990000");
        Assert.Same(IntegrationsController.PickByPhone([a, b], "5527999990000"),
                    IntegrationsController.PickByPhone([b, a], "5527999990000"));
    }

    [Fact]
    public async Task UnknownCustomer_ReturnsNotFound()
    {
        var sut = BuildController();
        _customerRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.WhatsAppEvent(
            ValidKey, Request(phone: "5527999990000", @event: "sent"), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UnknownEvent_ReturnsBadRequest()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent(
            ValidKey, Request(customerId: customer.Id, @event: "bogus"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task MissingCustomerIdAndPhone_ReturnsBadRequest()
    {
        var sut = BuildController();

        var result = await sut.WhatsAppEvent(ValidKey, Request(customerId: null, phone: null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ===== proposal_draft (bot pede rascunho; dono revisa e envia) =====

    [Fact]
    public async Task ProposalDraft_WithoutPackageId_ReturnsBadRequest()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await sut.WhatsAppEvent(ValidKey, Request(customerId: customer.Id, @event: "proposal_draft"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _proposalRepository.Verify(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProposalDraft_CreatesBotDraftForOwner()
    {
        var sut = BuildController();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _proposalRepository.Setup(r => r.GetByCustomerAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Proposal>());
        var pkg = new ServicePackage(OwnerUserId, "Site Essencial", "", "Página única", 1800m, 15, 0);
        _packageRepository.Setup(r => r.GetByIdAsync(pkg.Id, It.IsAny<CancellationToken>())).ReturnsAsync(pkg);
        Proposal? saved = null;
        _proposalRepository.Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Callback<Proposal, CancellationToken>((p, _) => saved = p);

        var request = new WhatsAppEventIntegrationRequest
        {
            CustomerId = customer.Id, Event = "proposal_draft", PackageId = pkg.Id, Summary = "Quer site para a padaria",
        };
        var result = await sut.WhatsAppEvent(ValidKey, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(saved);
        Assert.Equal(ProposalOrigin.Bot, saved!.Origin);
        Assert.Equal(ProposalStatus.Draft, saved.Status);
        Assert.Equal(OwnerUserId, saved.UserId);
        _taskRepository.Verify(r => r.AddAsync(
            It.Is<TaskItem>(t => t.Description == "Quer site para a padaria" && t.UserId == OwnerUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProposalDraft_WrongKey_ReturnsUnauthorized()
    {
        var sut = BuildController();
        var request = new WhatsAppEventIntegrationRequest { CustomerId = Guid.NewGuid(), Event = "proposal_draft", PackageId = Guid.NewGuid() };

        var result = await sut.WhatsAppEvent("wrong-key", request, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ===== GET integrations/service-packages (catálogo do bot) =====

    [Fact]
    public async Task ServicePackagesForBot_WrongKey_ReturnsUnauthorized()
    {
        var result = await BuildController().ServicePackagesForBot("wrong-key", CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(result);
        _packageRepository.Verify(r => r.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ServicePackagesForBot_NotConfigured_Returns503()
    {
        var result = await BuildController(configured: false).ServicePackagesForBot(ValidKey, CancellationToken.None);
        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task ServicePackagesForBot_ReturnsOwnersActivePackagesWithoutScope()
    {
        var pkg = new ServicePackage(OwnerUserId, "Site Essencial", "Site de 1 página", "Escopo interno", 1800m, 15, 0);
        _packageRepository.Setup(r => r.GetByUserAsync(OwnerUserId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServicePackage> { pkg });

        var result = await BuildController().ServicePackagesForBot(ValidKey, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("Site Essencial", json);
        Assert.Contains("1800", json);
        Assert.DoesNotContain("Escopo interno", json);
    }
}
