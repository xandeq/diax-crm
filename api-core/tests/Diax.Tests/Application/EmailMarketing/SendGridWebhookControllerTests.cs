using System.Linq.Expressions;
using System.Text.Json;
using Diax.Api.Controllers.V1;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// P1: SendGrid webhook controller tests — event mapping, bounce handling, suppression.
/// Guards: parse error returns 400, unknown items ignored, bounce creates suppression.
/// </summary>
public class SendGridWebhookControllerTests
{
    private readonly Mock<IEmailQueueRepository> _queueRepo = new();
    private readonly Mock<IEmailCampaignRepository> _campaignRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressionRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public SendGridWebhookControllerTests()
    {
    }

    private SendGridWebhookController CreateSut()
    {
        return new SendGridWebhookController(
            _queueRepo.Object,
            _campaignRepo.Object,
            _customerRepo.Object,
            _suppressionRepo.Object,
            _unitOfWork.Object,
            Options.Create(new SendGridSettings { WebhookSecret = "" }),
            new Mock<ILogger<SendGridWebhookController>>().Object);
    }

    private void SetupHttpContext(SendGridWebhookController controller, List<SendGridWebhookEvent> events)
    {
        var json = JsonSerializer.Serialize(events);
        var body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var httpContext = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();

        request.Setup(r => r.Body).Returns(body);
        request.Setup(r => r.Headers).Returns(new HeaderDictionary());

        httpContext.Setup(c => c.Request).Returns(request.Object);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    private void SetupHttpContextEmpty(SendGridWebhookController controller)
    {
        var httpContext = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();

        request.Setup(r => r.Body).Returns(new MemoryStream(new byte[0]));
        request.Setup(r => r.Headers).Returns(new HeaderDictionary());

        httpContext.Setup(c => c.Request).Returns(request.Object);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public async Task HandleWebhook_DeliveredEvent_MarkItemDeliveredAndIncrementsCount()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = "sg-msg-123";
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new SendGridWebhookEvent { Event = "delivered", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(item.DeliveredAt.HasValue);
        Assert.Equal(1, campaign.DeliveredCount);
        _campaignRepo.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_OpenEvent_RecordsOpenAndIncrementsCount()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = "sg-msg-456";
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new SendGridWebhookEvent { Event = "open", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.NotNull(item.OpenedAt);
        Assert.Equal(1, campaign.OpenCount);
        _campaignRepo.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_ClickEvent_IncrementsClick()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = "sg-msg-789";
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new SendGridWebhookEvent { Event = "click", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.ClickCount);
        _campaignRepo.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_DeferredEvent_DoesNothing()
    {
        var sut = CreateSut();
        var evt = new SendGridWebhookEvent { Event = "deferred", Email = "test@example.com", MessageId = "sg-msg-defer" };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _queueRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_BounceEvent_OptOutsAndCreatesSuppressionEntry()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "bounce@example.com";
        var messageId = "sg-msg-bounce";
        var customer = new Customer("Bounced", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Bounced", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, null);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        var evt = new SendGridWebhookEvent { Event = "bounce", Email = email, MessageId = messageId, BounceType = "permanent" };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(customer.EmailOptOut);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Email == email && s.Reason == SuppressionReason.HardBounce), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_DroppedEvent_IncrementsBounceButDoesNotOptOut()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var email = "dropped@example.com";
        var messageId = "sg-msg-drop";
        var customer = new Customer("Dropped", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Dropped", email, "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new SendGridWebhookEvent { Event = "dropped", Email = email, MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.False(customer.EmailOptOut); // Dropped does not opt out
        Assert.Equal(1, campaign.BounceCount); // But it increments bounce count
    }

    [Fact]
    public async Task HandleWebhook_SpamreportEvent_CreatesSuppressionAsSpamComplaint()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "spam@example.com";
        var customer = new Customer("Spam", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Spam", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, null);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        var evt = new SendGridWebhookEvent { Event = "spamreport", Email = email };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(customer.EmailOptOut);
        _customerRepo.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Reason == SuppressionReason.SpamComplaint), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_UnsubscribeEvent_CreatesSuppressionAsManualOptOut()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "unsub@example.com";
        var customer = new Customer("Unsub", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Unsub", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, null);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        var evt = new SendGridWebhookEvent { Event = "unsubscribe", Email = email };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(customer.EmailOptOut);
        _customerRepo.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Reason == SuppressionReason.ManualOptOut), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_GroupUnsubscribeEvent_CreatesSuppressionAsManualOptOut()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "groupunsub@example.com";
        var customer = new Customer("GroupUnsub", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "GroupUnsub", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, null);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        var evt = new SendGridWebhookEvent { Event = "group_unsubscribe", Email = email };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(customer.EmailOptOut);
        _customerRepo.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Reason == SuppressionReason.ManualOptOut), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_EmptyPayload_ReturnsBadRequest()
    {
        var sut = CreateSut();
        SetupHttpContextEmpty(sut);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task HandleWebhook_UnknownItemEvent_IsIgnored()
    {
        var sut = CreateSut();
        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var evt = new SendGridWebhookEvent { Event = "open", Email = "unknown@example.com", MessageId = "sg-unknown" };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _campaignRepo.Verify(r => r.UpdateAsync(It.IsAny<EmailCampaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_UnknownEventType_IsIgnored()
    {
        var sut = CreateSut();
        var evt = new SendGridWebhookEvent { Event = "unknown_event", Email = "test@example.com" };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _queueRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_NullMessageIdOnOpenEvent_ReturnsEarly()
    {
        var sut = CreateSut();
        var evt = new SendGridWebhookEvent { Event = "open", Email = "test@example.com", MessageId = null };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _queueRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_NullEmailOnOptOut_ReturnsEarly()
    {
        var sut = CreateSut();
        var evt = new SendGridWebhookEvent { Event = "spamreport", Email = null };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _customerRepo.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_OptOutEvent_OptOutsCustomer()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "optout@example.com";
        var customer = new Customer("OptOut", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "OptOut", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, null);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(It.IsAny<Guid>(), email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        var evt = new SendGridWebhookEvent { Event = "unsubscribe", Email = email };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(customer.EmailOptOut);
    }

    [Fact]
    public async Task HandleWebhook_EventTypeCaseInsensitive()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = "sg-msg-case";
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new SendGridWebhookEvent { Event = "DELIVERED", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(item.DeliveredAt.HasValue);
    }

    [Fact]
    public async Task HandleWebhook_BounceEvent_IncrementsBounceCampaignCount()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = "sg-msg-bounce-campaign";
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Bounced", "bounce@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var customer = new Customer("Bounced", "bounce@example.com");
        _customerRepo
            .Setup(r => r.GetByEmailAsync("bounce@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(It.IsAny<Guid>(), "bounce@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new SendGridWebhookEvent { Event = "bounce", Email = "bounce@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.BounceCount);
    }

    [Fact]
    public async Task HandleWebhook_MultipleEvents_ProcessesAll()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = "sg-msg-multi";
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var events = new List<SendGridWebhookEvent>
        {
            new() { Event = "delivered", Email = "test@example.com", MessageId = messageId },
            new() { Event = "open", Email = "test@example.com", MessageId = messageId }
        };
        SetupHttpContext(sut, events);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(item.DeliveredAt.HasValue); // delivered event processed
        Assert.NotNull(item.OpenedAt); // open event processed
        Assert.Equal(1, campaign.DeliveredCount);
        Assert.Equal(1, campaign.OpenCount);
    }
}
