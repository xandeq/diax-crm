using System.Linq.Expressions;
using Diax.Api.Controllers.V1;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// P1b: o webhook do Resend (email.clicked) avança o funil via
/// Customer.RegisterEngagement() — mesma regra do Brevo (click.ledger), mas
/// aqui a idempotência natural é a campanha resolvida via CampaignId do item.
/// </summary>
public class ResendWebhookControllerTests
{
    private readonly Mock<IEmailQueueRepository> _queueRepo = new();
    private readonly Mock<IEmailCampaignRepository> _campaignRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressionRepo = new();
    private readonly ResendWebhookController _sut;

    public ResendWebhookControllerTests()
    {
        _sut = new ResendWebhookController(
            _queueRepo.Object,
            _campaignRepo.Object,
            _customerRepo.Object,
            _suppressionRepo.Object,
            new Mock<IUnitOfWork>().Object,
            Options.Create(new ResendSettings { WebhookSecret = "" }), // permissive
            new Mock<ILogger<ResendWebhookController>>().Object);
    }

    private (EmailQueueItem Item, Guid CampaignId) SetupSentItem(Guid? customerId = null)
    {
        var campaignId = Guid.NewGuid();
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "lead@example.com", "Assunto", "corpo",
            DateTime.UtcNow, customerId ?? Guid.NewGuid(), null, campaignId, EmailProvider.Resend);
        item.MarkSent("resend-msg-id");

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Assunto", "<p>corpo</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        return (item, campaignId);
    }

    private static ResendWebhookPayload ClickPayload(string emailId) => new()
    {
        Type = "email.clicked",
        Data = new ResendWebhookData { EmailId = emailId, To = ["lead@example.com"] }
    };

    [Fact]
    public async Task Click_AdvancesLeadCustomerToQualified()
    {
        var (item, _) = SetupSentItem();
        var customer = new Customer("Lead", "lead@example.com");
        _customerRepo
            .Setup(r => r.GetByIdAsync(item.CustomerId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _sut.HandleWebhook(ClickPayload("resend-msg-id"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(CustomerStatus.Qualified, customer.Status);
        _customerRepo.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Click_ContactedCustomer_AdvancesToQualified()
    {
        var (item, _) = SetupSentItem();
        var customer = new Customer("Lead", "lead@example.com");
        customer.RegisterContact(); // já Contacted
        _customerRepo
            .Setup(r => r.GetByIdAsync(item.CustomerId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        await _sut.HandleWebhook(ClickPayload("resend-msg-id"), CancellationToken.None);

        Assert.Equal(CustomerStatus.Qualified, customer.Status);
    }

    [Fact]
    public async Task Click_CustomerMissing_DoesNotThrow_ClickStillCounted()
    {
        var (_, campaignId) = SetupSentItem();
        // _customerRepo.GetByIdAsync não configurado para este CustomerId → retorna null.

        var result = await _sut.HandleWebhook(ClickPayload("resend-msg-id"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _campaignRepo.Verify(c => c.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        _customerRepo.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Click_WithoutCampaign_DoesNotTouchCustomerRepository()
    {
        // Item avulso (sem CampaignId) — o guard de idempotência existente já retorna
        // cedo; RegisterEngagement não deve nem ser tentado.
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "lead@example.com", "Assunto", "corpo",
            DateTime.UtcNow, Guid.NewGuid(), null, null, EmailProvider.Resend);
        item.MarkSent("resend-msg-avulso");
        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var result = await _sut.HandleWebhook(ClickPayload("resend-msg-avulso"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _customerRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delivered_And_Opened_DoNotRegisterEngagement()
    {
        // Aberturas não contam (Apple MPP) — só o click deve chamar RegisterEngagement.
        var (item, _) = SetupSentItem();
        _customerRepo
            .Setup(r => r.GetByIdAsync(item.CustomerId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer("Lead", "lead@example.com"));

        var deliveredPayload = new ResendWebhookPayload
        {
            Type = "email.delivered",
            Data = new ResendWebhookData { EmailId = "resend-msg-id", To = ["lead@example.com"] }
        };
        var openedPayload = new ResendWebhookPayload
        {
            Type = "email.opened",
            Data = new ResendWebhookData { EmailId = "resend-msg-id", To = ["lead@example.com"] }
        };

        await _sut.HandleWebhook(deliveredPayload, CancellationToken.None);
        await _sut.HandleWebhook(openedPayload, CancellationToken.None);

        _customerRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
