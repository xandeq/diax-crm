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
/// P1: Mailjet webhook controller tests — event mapping, bounce handling, suppression.
/// Guards: parse error returns 200, unknown items ignored, HardBounce creates suppression.
/// </summary>
public class MailjetWebhookControllerTests
{
    private readonly Mock<IEmailQueueRepository> _queueRepo = new();
    private readonly Mock<IEmailCampaignRepository> _campaignRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressionRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public MailjetWebhookControllerTests()
    {
    }

    private MailjetWebhookController CreateSut()
    {
        return new MailjetWebhookController(
            _queueRepo.Object,
            _campaignRepo.Object,
            _customerRepo.Object,
            _suppressionRepo.Object,
            _unitOfWork.Object,
            Options.Create(new MailjetSettings { WebhookSecret = "" }),
            new Mock<ILogger<MailjetWebhookController>>().Object);
    }

    private void SetupHttpContext(MailjetWebhookController controller, List<MailjetWebhookEvent> events)
    {
        var json = JsonSerializer.Serialize(events);
        var body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var httpContext = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();

        request.Setup(r => r.Body).Returns(body);
        request.Setup(r => r.ContentLength).Returns(body.Length);
        request.Setup(r => r.Headers).Returns(new HeaderDictionary());

        httpContext.Setup(c => c.Request).Returns(request.Object);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    private void SetupHttpContextEmpty(MailjetWebhookController controller)
    {
        var httpContext = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();

        request.Setup(r => r.ContentLength).Returns((long?)null);
        request.Setup(r => r.Headers).Returns(new HeaderDictionary());

        httpContext.Setup(c => c.Request).Returns(request.Object);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public async Task HandleWebhook_SentEvent_IncrementsCampaignDelivered()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = 1234567890L;
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId.ToString());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new MailjetWebhookEvent { Event = "sent", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.DeliveredCount);
        _campaignRepo.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_OpenEvent_IncrementsCampaignAndRecordsItemOpen()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = 1234567890L;
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId.ToString());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new MailjetWebhookEvent { Event = "open", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.OpenCount);
        Assert.NotNull(item.OpenedAt);
        _campaignRepo.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_ClickEvent_IncrementsCampaignClick()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = 1234567890L;
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId.ToString());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new MailjetWebhookEvent { Event = "click", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.ClickCount);
        _campaignRepo.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_HardBounceEvent_OptOutsAndCreatesSuppressionEntry()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "bounce@example.com";
        var customer = new Customer("Bounced", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Bounced", email, "Subj", "Body",
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

        var evt = new MailjetWebhookEvent { Event = "bounce", Email = email, HardBounce = true };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.True(customer.EmailOptOut);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Email == email && s.Reason == SuppressionReason.HardBounce), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_SoftBounceEvent_DoesNotOptOut()
    {
        var sut = CreateSut();
        var email = "soft@example.com";
        var customer = new Customer("Soft", email);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var evt = new MailjetWebhookEvent { Event = "bounce", Email = email, HardBounce = false };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.False(customer.EmailOptOut);
    }

    [Fact]
    public async Task HandleWebhook_SpamEvent_CreatesSuppressionAsSpamComplaint()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "spam@example.com";
        var customer = new Customer("Spam", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Spam", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, Guid.NewGuid());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        _campaignRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>"));

        var evt = new MailjetWebhookEvent { Event = "spam", Email = email };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Reason == SuppressionReason.SpamComplaint), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_UnsubEvent_CreatesSuppressionAsManualOptOut()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "unsub@example.com";
        var customer = new Customer("Unsub", email);
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Unsub", email, "Subj", "Body",
            DateTime.UtcNow, userId, null, Guid.NewGuid());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _customerRepo
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _suppressionRepo
            .Setup(r => r.FindByEmailAsync(userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailSuppression?)null);

        _campaignRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>"));

        var evt = new MailjetWebhookEvent { Event = "unsub", Email = email };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _suppressionRepo.Verify(
            r => r.AddAsync(It.Is<EmailSuppression>(s => s.Reason == SuppressionReason.ManualOptOut), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_EmptyBody_ReturnsOkay()
    {
        var sut = CreateSut();
        SetupHttpContextEmpty(sut);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task HandleWebhook_UnknownItemEvent_IsIgnored()
    {
        var sut = CreateSut();
        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var evt = new MailjetWebhookEvent { Event = "open", Email = "unknown@example.com", MessageId = 999999L };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _campaignRepo.Verify(r => r.UpdateAsync(It.IsAny<EmailCampaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_UnknownEventType_IsIgnored()
    {
        var sut = CreateSut();
        var evt = new MailjetWebhookEvent { Event = "unknown_event", Email = "test@example.com" };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _queueRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_NullEmailOnBounceEvent_ReturnsEarly()
    {
        var sut = CreateSut();
        var evt = new MailjetWebhookEvent { Event = "bounce", Email = null };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _customerRepo.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_EventTypeCaseInsensitive()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = 1234567890L;
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId.ToString());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var evt = new MailjetWebhookEvent { Event = "OPEN", Email = "test@example.com", MessageId = messageId };
        SetupHttpContext(sut, [evt]);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.OpenCount);
    }

    [Fact]
    public async Task HandleWebhook_MultipleEvents_ProcessesAll()
    {
        var sut = CreateSut();
        var campaignId = Guid.NewGuid();
        var messageId = 1234567890L;
        var item = new EmailQueueItem(
            Guid.NewGuid(), "Lead", "test@example.com", "Subj", "Body",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        item.MarkSent(messageId.ToString());

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Subj", "<p>Body</p>");
        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var events = new List<MailjetWebhookEvent>
        {
            new() { Event = "sent", Email = "test@example.com", MessageId = messageId },
            new() { Event = "open", Email = "test@example.com", MessageId = messageId }
        };
        SetupHttpContext(sut, events);

        var result = await sut.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, campaign.DeliveredCount); // sent event processed
        Assert.NotNull(item.OpenedAt); // open event processed
        Assert.Equal(1, campaign.OpenCount);
    }
}
