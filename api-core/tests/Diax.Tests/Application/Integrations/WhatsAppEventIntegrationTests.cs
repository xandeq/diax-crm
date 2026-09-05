using System.Linq.Expressions;
using Diax.Api.Controllers.V1;
using Diax.Application.Briefings;
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
}
