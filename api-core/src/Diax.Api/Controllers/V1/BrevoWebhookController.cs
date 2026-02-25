using System.Text.Json.Serialization;
using Asp.Versioning;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/brevo")]
public class BrevoWebhookController : BaseApiController
{
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BrevoSettings _brevoSettings;
    private readonly ILogger<BrevoWebhookController> _logger;

    public BrevoWebhookController(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IOptions<BrevoSettings> brevoSettings,
        ILogger<BrevoWebhookController> logger)
    {
        _emailQueueRepository = emailQueueRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _brevoSettings = brevoSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Recebe webhooks do Brevo para tracking de e-mails (delivered, opened, click, bounce, spam, unsubscribed).
    /// </summary>
    [HttpPost("")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] BrevoWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        // Validate webhook secret if configured
        if (!string.IsNullOrWhiteSpace(_brevoSettings.WebhookSecret))
        {
            var signature = Request.Headers["X-Brevo-Signature"].FirstOrDefault()
                         ?? Request.Query["secret"].FirstOrDefault();

            if (signature != _brevoSettings.WebhookSecret)
            {
                _logger.LogWarning("Brevo webhook received with invalid signature");
                return Unauthorized();
            }
        }

        if (string.IsNullOrWhiteSpace(payload.Event))
        {
            _logger.LogWarning("Brevo webhook received with empty event");
            return BadRequest("Missing event type");
        }

        _logger.LogInformation(
            "Brevo webhook received: event={Event}, email={Email}, messageId={MessageId}, tag={Tag}",
            payload.Event, payload.Email, payload.MessageId, payload.Tag);

        try
        {
            switch (payload.Event.ToLowerInvariant())
            {
                case "delivered":
                    await HandleDeliveredAsync(payload, cancellationToken);
                    break;

                case "opened":
                    await HandleOpenedAsync(payload, cancellationToken);
                    break;

                case "click":
                    _logger.LogInformation(
                        "Click event for email={Email}, messageId={MessageId}",
                        payload.Email, payload.MessageId);
                    break;

                case "hard_bounce":
                case "soft_bounce":
                    await HandleBounceAsync(payload, cancellationToken);
                    break;

                case "spam":
                    await HandleOptOutAsync(payload, "spam", cancellationToken);
                    break;

                case "unsubscribed":
                    await HandleOptOutAsync(payload, "unsubscribed", cancellationToken);
                    break;

                default:
                    _logger.LogInformation("Unhandled Brevo event: {Event}", payload.Event);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing Brevo webhook: event={Event}, email={Email}",
                payload.Event, payload.Email);
            // Return OK to prevent Brevo from retrying — log the error for investigation
        }

        return Ok();
    }

    private async Task HandleDeliveredAsync(BrevoWebhookPayload payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.MessageId))
            return;

        var items = await _emailQueueRepository.FindAsync(
            q => q.ProviderMessageId == payload.MessageId,
            cancellationToken);

        var queueItem = items.FirstOrDefault();
        if (queueItem == null)
        {
            _logger.LogDebug(
                "No queue item found for delivered event with ProviderMessageId={MessageId}",
                payload.MessageId);
            return;
        }

        _logger.LogInformation(
            "Email delivered: QueueItemId={QueueItemId}, ProviderMessageId={MessageId}",
            queueItem.Id, payload.MessageId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleOpenedAsync(BrevoWebhookPayload payload, CancellationToken cancellationToken)
    {
        // Try to find campaign by tag (which contains the campaign ID)
        if (!string.IsNullOrWhiteSpace(payload.Tag) && Guid.TryParse(payload.Tag, out var campaignId))
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(campaignId, cancellationToken);
            if (campaign != null)
            {
                campaign.IncrementOpened();
                await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Open count incremented for CampaignId={CampaignId}, new OpenCount={OpenCount}",
                    campaignId, campaign.OpenCount);
            }
            else
            {
                _logger.LogWarning(
                    "Campaign not found for opened event: CampaignId={CampaignId}",
                    campaignId);
            }
        }
        else
        {
            _logger.LogDebug(
                "Opened event without valid campaign tag: email={Email}, tag={Tag}",
                payload.Email, payload.Tag);
        }
    }

    private async Task HandleBounceAsync(BrevoWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Bounce event ({Event}) for email={Email}",
            payload.Event, payload.Email);

        // Hard bounces should opt-out the customer
        if (payload.Event.Equals("hard_bounce", StringComparison.OrdinalIgnoreCase))
        {
            await HandleOptOutAsync(payload, "hard_bounce", cancellationToken);
        }
    }

    private async Task HandleOptOutAsync(
        BrevoWebhookPayload payload,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
            return;

        var customer = await _customerRepository.GetByEmailAsync(payload.Email, cancellationToken);
        if (customer == null)
        {
            _logger.LogDebug(
                "No customer found for opt-out ({Reason}): email={Email}",
                reason, payload.Email);
            return;
        }

        if (!customer.EmailOptOut)
        {
            customer.OptOutEmail();
            await _customerRepository.UpdateAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Customer opted out of email ({Reason}): CustomerId={CustomerId}, Email={Email}",
                reason, customer.Id, payload.Email);
        }
        else
        {
            _logger.LogDebug(
                "Customer already opted out: CustomerId={CustomerId}, Email={Email}",
                customer.Id, payload.Email);
        }
    }
}

/// <summary>
/// Payload recebido via webhook do Brevo.
/// </summary>
public class BrevoWebhookPayload
{
    /// <summary>
    /// Tipo do evento: delivered, opened, click, hard_bounce, soft_bounce, spam, unsubscribed.
    /// </summary>
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// E-mail do destinatário.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// ID da mensagem no provedor (Brevo).
    /// </summary>
    [JsonPropertyName("message-id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// Timestamp epoch do evento.
    /// </summary>
    [JsonPropertyName("ts_epoch")]
    public long? TsEpoch { get; set; }

    /// <summary>
    /// Tag associada (geralmente o campaign ID).
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}
