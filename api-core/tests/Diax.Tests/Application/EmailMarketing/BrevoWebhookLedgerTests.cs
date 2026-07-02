using System.Linq.Expressions;
using Diax.Api.Controllers.V1;
using Diax.Application.EmailMarketing;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Idempotência dos webhooks de bounce/unsubscribe via ledger email_events (P1-12):
/// reentrega do mesmo evento pelo Brevo não pode contar em dobro. Antes, bounce e
/// unsubscribe não tinham NENHUMA guarda.
/// </summary>
public class BrevoWebhookLedgerTests
{
    private readonly Mock<IEmailQueueRepository> _queueRepo = new();
    private readonly Mock<IEmailCampaignRepository> _campaignRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmailEventRepository> _eventRepo = new();
    private readonly List<EmailEvent> _ledger = [];
    private readonly BrevoWebhookController _sut;

    public BrevoWebhookLedgerTests()
    {
        // Ledger fake: Exists reflete o que foi adicionado — simula o índice único.
        _eventRepo
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<EmailEventType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, EmailEventType type, CancellationToken _) =>
                _ledger.Any(e => e.QueueItemId == id && e.EventType == type));
        _eventRepo
            .Setup(r => r.AddAsync(It.IsAny<EmailEvent>(), It.IsAny<CancellationToken>()))
            .Callback<EmailEvent, CancellationToken>((e, _) => _ledger.Add(e))
            .Returns(Task.CompletedTask);

        _sut = new BrevoWebhookController(
            _queueRepo.Object,
            _campaignRepo.Object,
            _customerRepo.Object,
            new Mock<IUnitOfWork>().Object,
            Options.Create(new BrevoSettings { WebhookSecret = "" }), // permissive
            new Mock<ILogger<BrevoWebhookController>>().Object,
            new Mock<IPilotCircuitBreaker>().Object,
            new Mock<IAuditLogRepository>().Object,
            new Mock<IUserRepository>().Object,
            _eventRepo.Object);
    }

    private (EmailQueueItem Item, EmailCampaign Campaign, string MessageId) SetupSentItem()
    {
        var messageId = "202607011234.abc@smtp-relay.mailin.fr";
        var campaign = new EmailCampaign(Guid.NewGuid(), "Camp", "Assunto", "corpo {{unsubscribe_url}}");
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "lead@example.com", "Assunto", "corpo",
            DateTime.UtcNow, Guid.NewGuid(), null, campaign.Id, EmailProvider.Brevo);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
        _customerRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        return (item, campaign, messageId);
    }

    private BrevoWebhookPayload Payload(string @event, string messageId, Guid campaignId) => new()
    {
        Event = @event,
        Email = "lead@example.com",
        MessageId = messageId,
        Tag = campaignId.ToString()
    };

    [Fact]
    public async Task HardBounce_DeliveredTwice_CountsBounceOnce()
    {
        var (_, campaign, messageId) = SetupSentItem();
        var payload = Payload("hard_bounce", messageId, campaign.Id);

        await _sut.HandleWebhook(payload, CancellationToken.None);
        await _sut.HandleWebhook(payload, CancellationToken.None); // reentrega do Brevo

        Assert.Equal(1, campaign.BounceCount);
        Assert.Single(_ledger, e => e.EventType == EmailEventType.Bounced);
    }

    [Fact]
    public async Task Unsubscribed_DeliveredTwice_CountsOnce()
    {
        var (_, campaign, messageId) = SetupSentItem();
        var payload = Payload("unsubscribed", messageId, campaign.Id);

        await _sut.HandleWebhook(payload, CancellationToken.None);
        await _sut.HandleWebhook(payload, CancellationToken.None);

        Assert.Equal(1, campaign.UnsubscribeCount);
        Assert.Single(_ledger, e => e.EventType == EmailEventType.Unsubscribed);
    }

    [Fact]
    public async Task HardBounce_DoesNotIncrementUnsubscribeCounter()
    {
        // Antes, o hard_bounce passava pelo HandleOptOut e inflava UnsubscribeCount —
        // bounce tem contador próprio; unsubscribe é só spam/unsubscribed.
        var (_, campaign, messageId) = SetupSentItem();

        await _sut.HandleWebhook(Payload("hard_bounce", messageId, campaign.Id), CancellationToken.None);

        Assert.Equal(1, campaign.BounceCount);
        Assert.Equal(0, campaign.UnsubscribeCount);
    }

    [Fact]
    public async Task Click_DeliveredTwice_CountsOnce_ViaLedgerNotAuditScan()
    {
        var (_, campaign, messageId) = SetupSentItem();
        var payload = Payload("click", messageId, campaign.Id);

        await _sut.HandleWebhook(payload, CancellationToken.None);
        await _sut.HandleWebhook(payload, CancellationToken.None);

        Assert.Equal(1, campaign.ClickCount);
        Assert.Single(_ledger, e => e.EventType == EmailEventType.Clicked);
    }

    [Fact]
    public async Task Spam_RecordsSpamEventType()
    {
        var (_, campaign, messageId) = SetupSentItem();

        await _sut.HandleWebhook(Payload("spam", messageId, campaign.Id), CancellationToken.None);

        Assert.Single(_ledger, e => e.EventType == EmailEventType.Spam);
        Assert.Equal(1, campaign.UnsubscribeCount);
    }
}
