using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Diax.Application.EmailMarketing;
using Diax.Domain.Audit;
using Diax.Domain.Auth;

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

    private readonly IPilotCircuitBreaker _circuitBreaker;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
 
    public BrevoWebhookController(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IOptions<BrevoSettings> brevoSettings,
        ILogger<BrevoWebhookController> logger,
        IPilotCircuitBreaker circuitBreaker,
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository)
    {
        _emailQueueRepository = emailQueueRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _brevoSettings = brevoSettings.Value;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
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
        // Validar webhook secret apenas se configurado (permissive mode quando vazio)
        if (!string.IsNullOrWhiteSpace(_brevoSettings.WebhookSecret))
        {
            var signature = Request.Headers["X-Brevo-Signature"].FirstOrDefault()
                         ?? Request.Query["secret"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Brevo webhook recebido sem assinatura");
                return Unauthorized();
            }

            // Comparação em tempo constante para evitar timing attacks
            var expectedBytes = Encoding.UTF8.GetBytes(_brevoSettings.WebhookSecret);
            var actualBytes = Encoding.UTF8.GetBytes(signature);
            if (!CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
            {
                _logger.LogWarning("Brevo webhook recebido com assinatura inválida");
                return Unauthorized();
            }
        }
        else
        {
            _logger.LogDebug("Brevo WebhookSecret não configurado — validação de assinatura ignorada.");
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
                    await HandleClickAsync(payload, cancellationToken);
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
            _circuitBreaker.ResetWebhookFailure();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing Brevo webhook: event={Event}, email={Email}",
                payload.Event, payload.Email);
            _circuitBreaker.RecordWebhookFailure();
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

        if (queueItem.DeliveredAt.HasValue)
        {
            _logger.LogInformation("Delivered event ignored (already delivered): ProviderMessageId={MessageId}", payload.MessageId);
            return;
        }

        // Note: the Delivered event also sets the timestamp on the individual queue item
        queueItem.MarkDelivered();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Increment delivered count for campaign if exists
        if (queueItem.CampaignId.HasValue)
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(queueItem.CampaignId.Value, cancellationToken);
            if (campaign != null)
            {
                campaign.IncrementDelivered();
                await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Email delivered: QueueItemId={QueueItemId}, ProviderMessageId={MessageId}",
            queueItem.Id, payload.MessageId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleOpenedAsync(BrevoWebhookPayload payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.MessageId))
            return;

        var items = await _emailQueueRepository.FindAsync(
            q => q.ProviderMessageId == payload.MessageId,
            cancellationToken);

        var queueItem = items.FirstOrDefault();

        if (queueItem != null)
        {
            if (queueItem.OpenedAt.HasValue)
            {
                _logger.LogInformation("Opened event ignored (already opened): ProviderMessageId={MessageId}", payload.MessageId);
                return;
            }
            queueItem.RecordOpen();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Email opened: QueueItemId={QueueItemId}, ProviderMessageId={MessageId}, ReadCount={ReadCount}",
                queueItem.Id, payload.MessageId, queueItem.ReadCount);
        }

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

    private async Task HandleClickAsync(BrevoWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Click event for email={Email}, messageId={MessageId}, tag={Tag}",
            payload.Email, payload.MessageId, payload.Tag);
 
        if (string.IsNullOrWhiteSpace(payload.MessageId))
            return;

        // Try to find campaign by tag
        if (!string.IsNullOrWhiteSpace(payload.Tag) && Guid.TryParse(payload.Tag, out var campaignId))
        {
            // Idempotency check: check if click already logged for this messageId
            var logs = await _auditLogRepository.GetByResourceAsync("PilotCampaign", campaignId.ToString(), cancellationToken);
            var alreadyClicked = logs.Any(l => l.Description.Contains("PilotEmailClicked") && l.NewValues != null && l.NewValues.Contains(payload.MessageId));
            if (alreadyClicked)
            {
                _logger.LogInformation("Click event ignored (already clicked): ProviderMessageId={MessageId}", payload.MessageId);
                return;
            }

            var campaign = await _emailCampaignRepository.GetByIdAsync(campaignId, cancellationToken);
            if (campaign != null)
            {
                campaign.IncrementClick();
                await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
 
                // Log click audit event
                await LogPilotEventAsync(
                    "PilotEmailClicked",
                    "Success",
                    campaignId,
                    1,
                    false,
                    $"Email click detectado para {payload.Email}. MsgId: {payload.MessageId}",
                    cancellationToken,
                    payload.MessageId);

                _logger.LogInformation(
                    "Click count incremented for CampaignId={CampaignId}, new ClickCount={ClickCount}",
                    campaignId, campaign.ClickCount);
            }
        }
    }

    private async Task HandleBounceAsync(BrevoWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Bounce event ({Event}) for email={Email}, tag={Tag}",
            payload.Event, payload.Email, payload.Tag);
 
        Guid? campaignId = null;
        // Increment bounce count for campaign if exists
        if (!string.IsNullOrWhiteSpace(payload.Tag) && Guid.TryParse(payload.Tag, out var parsedId))
        {
            campaignId = parsedId;
            var campaign = await _emailCampaignRepository.GetByIdAsync(campaignId.Value, cancellationToken);
            if (campaign != null)
            {
                campaign.IncrementBounce();
                await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
            }
        }
 
        // Hard bounces should opt-out (suppress) the customer. They must NOT open the
        // circuit breaker on the first occurrence — a single dead recipient is normal list
        // attrition and halting all outbound is disproportionate. RecordWebhookFailure only
        // trips the breaker after 3 bounces (and RecordSuccess resets the counter), so a real
        // bad-list pattern is still caught while one-off bounces just get suppressed.
        if (payload.Event.Equals("hard_bounce", StringComparison.OrdinalIgnoreCase))
        {
            await HandleOptOutAsync(payload, "hard_bounce", cancellationToken);

            var wasClosed = !_circuitBreaker.IsOpen;
            _circuitBreaker.RecordWebhookFailure();

            if (campaignId.HasValue && wasClosed && _circuitBreaker.IsOpen)
            {
                await LogPilotEventAsync(
                    "PilotCircuitBreakerOpened",
                    "Failed",
                    campaignId.Value,
                    1,
                    false,
                    $"Circuit Breaker aberto após múltiplos hard bounces (último: {payload.Email})",
                    cancellationToken,
                    payload.MessageId);
            }
        }
 
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleOptOutAsync(
        BrevoWebhookPayload payload,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
            return;

        // Increment unsubscribe count for campaign if exists
        if (!string.IsNullOrWhiteSpace(payload.Tag) && Guid.TryParse(payload.Tag, out var campaignId))
        {
            var campaign = await _emailCampaignRepository.GetByIdAsync(campaignId, cancellationToken);
            if (campaign != null)
            {
                campaign.IncrementUnsubscribe();
                await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
            }
        }

        var customer = await _customerRepository.GetByEmailAsync(payload.Email, cancellationToken);
        if (customer == null)
        {
            _logger.LogDebug(
                "No customer found for opt-out ({Reason}): email={Email}",
                reason, payload.Email);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task LogPilotEventAsync(
        string action,
        string result,
        Guid campaignId,
        int leadCount,
        bool dryRun,
        string? blockingReasons,
        CancellationToken cancellationToken,
        string? correlationId = null)
    {
        Guid? userId = null;
        string userEmail = "sistema";
        
        var campaign = await _emailCampaignRepository.GetByIdAsync(campaignId, cancellationToken);
        if (campaign != null)
        {
            userId = campaign.UserId;
            var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user != null)
            {
                userEmail = user.Email;
            }
        }

        var details = new
        {
            CampaignId = campaignId,
            UserId = userId,
            UserEmail = userEmail,
            Action = action,
            Result = result,
            BlockingReasons = blockingReasons,
            LeadCount = leadCount,
            DryRun = dryRun,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            TimestampUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        var entry = AuditLogEntry.Create(
            userId,
            AuditAction.Custom,
            "PilotCampaign",
            campaignId.ToString(),
            $"Pilot campaign event: {action} ({result})",
            newValues: JsonSerializer.Serialize(details),
            correlationId: correlationId
        );

        await _auditLogRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
