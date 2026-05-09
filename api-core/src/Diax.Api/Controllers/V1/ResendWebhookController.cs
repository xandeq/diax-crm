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

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/resend")]
public class ResendWebhookController : BaseApiController
{
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendWebhookController> _logger;

    public ResendWebhookController(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        IEmailSuppressionRepository suppressionRepository,
        IUnitOfWork unitOfWork,
        IOptions<ResendSettings> settings,
        ILogger<ResendWebhookController> logger)
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
        [FromBody] ResendWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            var svixId = Request.Headers["svix-id"].FirstOrDefault();
            var svixTimestamp = Request.Headers["svix-timestamp"].FirstOrDefault();
            var svixSignature = Request.Headers["svix-signature"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(svixSignature))
                return Unauthorized();

            // Simple validation: verify HMAC over "svix-id.svix-timestamp.body"
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms, cancellationToken);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            var toSign = $"{svixId}.{svixTimestamp}.{body}";
            using var hmac = new HMACSHA256(Convert.FromBase64String(_settings.WebhookSecret));
            var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign)));

            var signatures = svixSignature.Split(' ');
            var valid = signatures.Any(s =>
            {
                var sigPart = s.StartsWith("v1,") ? s[3..] : s;
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computed),
                    Encoding.UTF8.GetBytes(sigPart));
            });

            if (!valid) return Unauthorized();
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.Type))
            return BadRequest();

        _logger.LogInformation(
            "Resend webhook: type={Type}, emailId={EmailId}",
            payload.Type, payload.Data?.EmailId);

        try
        {
            await ProcessEventAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Resend webhook: {Type}", payload.Type);
        }

        return Ok();
    }

    private async Task ProcessEventAsync(ResendWebhookPayload payload, CancellationToken ct)
    {
        var email = payload.Data?.To?.FirstOrDefault();
        var providerMessageId = payload.Data?.EmailId;

        switch (payload.Type.ToLowerInvariant())
        {
            case "email.delivered":
                await HandleDeliveredAsync(providerMessageId, ct);
                break;

            case "email.opened":
                await HandleOpenedAsync(providerMessageId, ct);
                break;

            case "email.clicked":
                await HandleClickedAsync(providerMessageId, ct);
                break;

            case "email.bounced":
                if (!string.IsNullOrWhiteSpace(email))
                    await HandleBounceAsync(email, ct);
                break;

            case "email.complained":
                if (!string.IsNullOrWhiteSpace(email))
                    await HandleSpamAsync(email, ct);
                break;

            case "email.unsubscribed":
                if (!string.IsNullOrWhiteSpace(email))
                    await HandleOptOutAsync(email, SuppressionReason.ManualOptOut, ct);
                break;
        }
    }

    private async Task HandleDeliveredAsync(string? providerMessageId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId)) return;

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == providerMessageId, ct);
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
    }

    private async Task HandleOpenedAsync(string? providerMessageId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId)) return;

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == providerMessageId, ct);
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

    private async Task HandleClickedAsync(string? providerMessageId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId)) return;

        var items = await _emailQueueRepository.FindAsync(q => q.ProviderMessageId == providerMessageId, ct);
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

    private async Task HandleBounceAsync(string email, CancellationToken ct)
    {
        _logger.LogWarning("Resend hard bounce for {Email}", email);
        await HandleOptOutAsync(email, SuppressionReason.HardBounce, ct);
    }

    private async Task HandleSpamAsync(string email, CancellationToken ct)
    {
        await HandleOptOutAsync(email, SuppressionReason.SpamComplaint, ct);
    }

    private async Task HandleOptOutAsync(string email, SuppressionReason reason, CancellationToken ct)
    {
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
                    EmailSuppression.ForEmail(userId.Value, email, reason, "resend_webhook"),
                    ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}

public class ResendWebhookPayload
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public ResendWebhookData? Data { get; set; }
}

public class ResendWebhookData
{
    [JsonPropertyName("email_id")]
    public string? EmailId { get; set; }

    [JsonPropertyName("to")]
    public List<string>? To { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
}
