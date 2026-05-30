using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Diax.Application.EmailMarketing.Pro;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/mailjet")]
public class MailjetWebhookController : BaseApiController
{
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MailjetSettings _settings;
    private readonly ILogger<MailjetWebhookController> _logger;

    public MailjetWebhookController(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        IEmailSuppressionRepository suppressionRepository,
        IUnitOfWork unitOfWork,
        IOptions<MailjetSettings> settings,
        ILogger<MailjetWebhookController> logger)
    {
        _emailQueueRepository = emailQueueRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _customerRepository = customerRepository;
        _suppressionRepository = suppressionRepository;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost("")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] List<MailjetWebhookEvent> events,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            var token = Request.Headers["X-Mailjet-Token"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var expectedBytes = Encoding.UTF8.GetBytes(_settings.WebhookSecret);
            var actualBytes = Encoding.UTF8.GetBytes(token);
            if (!CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
                return Unauthorized();
        }

        foreach (var evt in events ?? [])
        {
            _logger.LogInformation(
                "Mailjet webhook: event={Event}, email={Email}, messageId={MessageId}",
                evt.Event, evt.Email, evt.MessageId);
            try
            {
                await ProcessEventAsync(evt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Mailjet event: {Event}, email={Email}", evt.Event, evt.Email);
            }
        }

        return Ok();
    }

    private async Task ProcessEventAsync(MailjetWebhookEvent evt, CancellationToken ct)
    {
        switch (evt.Event?.ToLowerInvariant())
        {
            case "sent":
            case "open":
            case "click":
                await HandleStatEventAsync(evt, ct);
                break;

            case "bounce":
                await HandleBounceAsync(evt, ct);
                break;

            case "spam":
            case "unsub":
                await HandleOptOutAsync(evt, ct);
                break;
        }
    }

    private async Task HandleStatEventAsync(MailjetWebhookEvent evt, CancellationToken ct)
    {
        if (evt.MessageId == null) return;
        var msgId = evt.MessageId.ToString();

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == msgId, ct);
        var item = items.FirstOrDefault();

        if (item == null) return;

        switch (evt.Event?.ToLowerInvariant())
        {
            case "open":
                item.RecordOpen();
                break;
        }

        if (item.CampaignId.HasValue)
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(item.CampaignId.Value, ct);
            if (campaign != null)
            {
                switch (evt.Event?.ToLowerInvariant())
                {
                    case "open": campaign.IncrementOpened(); break;
                    case "click": campaign.IncrementClick(); break;
                    case "sent": campaign.IncrementDelivered(); break;
                }
                await _emailCampaignRepository.UpdateAsync(campaign, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandleBounceAsync(MailjetWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.Email)) return;

        _logger.LogWarning("Mailjet bounce for {Email}", evt.Email);

        if (evt.HardBounce == true)
        {
            var userId = await GetUserIdFromEmailAsync(evt.Email, ct);
            if (userId.HasValue)
            {
                var existing = await _suppressionRepository.FindByEmailAsync(userId.Value, evt.Email, ct);
                if (existing == null)
                {
                    await _suppressionRepository.AddAsync(
                        EmailSuppression.ForEmail(userId.Value, evt.Email, SuppressionReason.HardBounce, "mailjet_webhook"),
                        ct);
                }
            }

            var customer = await _customerRepository.GetByEmailAsync(evt.Email, ct);
            if (customer != null && !customer.EmailOptOut)
            {
                customer.OptOutEmail();
                await _customerRepository.UpdateAsync(customer, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandleOptOutAsync(MailjetWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.Email)) return;

        // Increment unsubscribe count for campaign if resolvable via queue item
        var items = await _emailQueueRepository.FindAsync(q => q.RecipientEmail == evt.Email, ct);
        var queueItem = items.OrderByDescending(i => i.SentAt).FirstOrDefault();
        if (queueItem?.CampaignId != null)
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(queueItem.CampaignId.Value, ct);
            if (campaign != null)
            {
                campaign.IncrementUnsubscribe();
                await _emailCampaignRepository.UpdateAsync(campaign, ct);
            }
        }

        var customer = await _customerRepository.GetByEmailAsync(evt.Email, ct);
        if (customer != null && !customer.EmailOptOut)
        {
            customer.OptOutEmail();
            await _customerRepository.UpdateAsync(customer, ct);
        }

        var userId = await GetUserIdFromEmailAsync(evt.Email, ct);
        if (userId.HasValue)
        {
            var existing = await _suppressionRepository.FindByEmailAsync(userId.Value, evt.Email, ct);
            if (existing == null)
            {
                var reason = evt.Event?.ToLowerInvariant() == "spam"
                    ? SuppressionReason.SpamComplaint
                    : SuppressionReason.ManualOptOut;
                await _suppressionRepository.AddAsync(
                    EmailSuppression.ForEmail(userId.Value, evt.Email, reason, "mailjet_webhook"),
                    ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Guid?> GetUserIdFromEmailAsync(string recipientEmail, CancellationToken ct)
    {
        var customer = await _customerRepository.GetByEmailAsync(recipientEmail, ct);
        if (customer == null) return null;

        var items = await _emailQueueRepository.FindAsync(q => q.RecipientEmail == recipientEmail, ct);
        return items.FirstOrDefault()?.UserId;
    }
}

public class MailjetWebhookEvent
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mj_message_id")]
    public long? MessageId { get; set; }

    [JsonPropertyName("hard_bounce")]
    public bool? HardBounce { get; set; }

    [JsonPropertyName("mj_campaign_id")]
    public long? CampaignId { get; set; }
}
