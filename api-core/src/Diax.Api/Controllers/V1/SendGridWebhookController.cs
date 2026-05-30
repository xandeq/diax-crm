using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Recebe eventos de webhook do SendGrid (Event Webhook).
/// SendGrid envia um array JSON de eventos para um único POST.
/// Configurar em: https://app.sendgrid.com/settings/mail_settings/event_notification
/// URL: https://api.alexandrequeiroz.com.br/api/v1/webhooks/sendgrid
/// Eventos suportados: delivered, open, click, bounce, spamreport, unsubscribe, group_unsubscribe, deferred, dropped.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/sendgrid")]
public class SendGridWebhookController : BaseApiController
{
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridWebhookController> _logger;

    public SendGridWebhookController(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        IEmailSuppressionRepository suppressionRepository,
        IUnitOfWork unitOfWork,
        IOptions<SendGridSettings> settings,
        ILogger<SendGridWebhookController> logger)
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
        [FromBody] List<SendGridWebhookEvent> events,
        CancellationToken cancellationToken)
    {
        // SendGrid signed webhook validation (optional — only if WebhookSecret configured)
        if (!string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            var signature = Request.Headers["X-Twilio-Email-Event-Webhook-Signature"].FirstOrDefault();
            var timestamp = Request.Headers["X-Twilio-Email-Event-Webhook-Timestamp"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
            {
                _logger.LogWarning("SendGrid webhook sem assinatura obrigatória");
                return Unauthorized();
            }
            // Basic ECDSA verification skipped here for simplicity — log and proceed
            _logger.LogDebug("SendGrid webhook signature present (full ECDSA verification not yet implemented)");
        }

        foreach (var evt in events ?? [])
        {
            _logger.LogInformation(
                "SendGrid webhook: event={Event}, email={Email}, messageId={MessageId}",
                evt.Event, evt.Email, evt.MessageId);
            try
            {
                await ProcessEventAsync(evt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SendGrid event: {Event}, email={Email}", evt.Event, evt.Email);
            }
        }

        return Ok();
    }

    private async Task ProcessEventAsync(SendGridWebhookEvent evt, CancellationToken ct)
    {
        switch (evt.Event?.ToLowerInvariant())
        {
            case "delivered":
                await HandleDeliveredAsync(evt, ct);
                break;

            case "open":
                await HandleOpenAsync(evt, ct);
                break;

            case "click":
                await HandleClickAsync(evt, ct);
                break;

            case "bounce":
            case "dropped":
                await HandleBounceAsync(evt, ct);
                break;

            case "spamreport":
                await HandleOptOutAsync(evt.Email, SuppressionReason.SpamComplaint, ct);
                break;

            case "unsubscribe":
            case "group_unsubscribe":
                await HandleOptOutAsync(evt.Email, SuppressionReason.ManualOptOut, ct);
                break;

            case "deferred":
                _logger.LogInformation("SendGrid deferred for {Email} — no action needed", evt.Email);
                break;
        }
    }

    private async Task HandleDeliveredAsync(SendGridWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.MessageId)) return;

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == evt.MessageId, ct);
        var item = items.FirstOrDefault();
        if (item == null) return;

        item.MarkDelivered();

        if (item.CampaignId.HasValue)
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(item.CampaignId.Value, ct);
            if (campaign != null)
            {
                campaign.IncrementDelivered();
                await _emailCampaignRepository.UpdateAsync(campaign, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("SendGrid delivered: {Email}, msgId={MsgId}", evt.Email, evt.MessageId);
    }

    private async Task HandleOpenAsync(SendGridWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.MessageId)) return;

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == evt.MessageId, ct);
        var item = items.FirstOrDefault();
        if (item == null) return;

        item.RecordOpen();

        if (item.CampaignId.HasValue)
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(item.CampaignId.Value, ct);
            if (campaign != null)
            {
                campaign.IncrementOpened();
                await _emailCampaignRepository.UpdateAsync(campaign, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandleClickAsync(SendGridWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.MessageId)) return;

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == evt.MessageId, ct);
        var item = items.FirstOrDefault();
        if (item?.CampaignId == null) return;

        var campaign = await _emailCampaignRepository.GetByIdAsync(item.CampaignId.Value, ct);
        if (campaign != null)
        {
            campaign.IncrementClick();
            await _emailCampaignRepository.UpdateAsync(campaign, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandleBounceAsync(SendGridWebhookEvent evt, CancellationToken ct)
    {
        _logger.LogWarning("SendGrid bounce ({Event}) for {Email}", evt.Event, evt.Email);

        if (!string.IsNullOrWhiteSpace(evt.MessageId))
        {
            var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == evt.MessageId, ct);
            var item = items.FirstOrDefault();
            if (item?.CampaignId != null)
            {
                var campaign = await _emailCampaignRepository.GetByIdAsync(item.CampaignId.Value, ct);
                if (campaign != null)
                {
                    campaign.IncrementBounce();
                    await _emailCampaignRepository.UpdateAsync(campaign, ct);
                }
            }
        }

        // Hard bounce → opt out
        if (evt.Event?.ToLowerInvariant() == "bounce")
            await HandleOptOutAsync(evt.Email, SuppressionReason.HardBounce, ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandleOptOutAsync(string? email, SuppressionReason reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var customer = await _customerRepository.GetByEmailAsync(email, ct);
        if (customer != null && !customer.EmailOptOut)
        {
            customer.OptOutEmail();
            await _customerRepository.UpdateAsync(customer, ct);
        }

        var items = await _emailQueueRepository.FindAsync(q => q.RecipientEmail == email, ct);
        var userId = items.FirstOrDefault()?.UserId;
        if (userId.HasValue)
        {
            var existing = await _suppressionRepository.FindByEmailAsync(userId.Value, email, ct);
            if (existing == null)
            {
                await _suppressionRepository.AddAsync(
                    EmailSuppression.ForEmail(userId.Value, email, reason, "sendgrid_webhook"),
                    ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Representa um evento individual do SendGrid Event Webhook.
/// Ref: https://docs.sendgrid.com/for-developers/tracking-events/event
/// </summary>
public class SendGridWebhookEvent
{
    /// <summary>Tipo do evento: delivered, open, click, bounce, spamreport, unsubscribe, deferred, dropped.</summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>Email do destinatário.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Message ID do SendGrid (sg_message_id).</summary>
    [JsonPropertyName("sg_message_id")]
    public string? MessageId { get; set; }

    /// <summary>Timestamp Unix do evento.</summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>URL clicada (evento click).</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Tipo de bounce: bounce, blocked.</summary>
    [JsonPropertyName("type")]
    public string? BounceType { get; set; }
}
