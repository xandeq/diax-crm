using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.EmailMarketing.Pro;
using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Diax.Api.Controllers.V1;

[Authorize]
[RequirePermission("campaigns.manage")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/email-providers")]
[Produces("application/json")]
public class EmailProvidersController : BaseApiController
{
    private readonly IProviderHealthService _healthService;
    private readonly ISmartPreselectionService _preselectService;
    private readonly EmailMarketingService _emailMarketingService;
    private readonly IProviderCircuitBreaker _providerBreaker;
    private readonly IPilotCircuitBreaker _pilotBreaker;
    private readonly IEmailProviderPolicy _providerPolicy;
    private readonly IEmailQueueRepository _queueRepository;
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITelegramSender _telegramSender;
    private readonly EmailSettings _emailSettings;
    private readonly IOptionsMonitor<EmailChainOptions> _chainOptions;

    public EmailProvidersController(
        IProviderHealthService healthService,
        ISmartPreselectionService preselectService,
        EmailMarketingService emailMarketingService,
        IProviderCircuitBreaker providerBreaker,
        IPilotCircuitBreaker pilotBreaker,
        IEmailProviderPolicy providerPolicy,
        IEmailQueueRepository queueRepository,
        IEmailCampaignRepository campaignRepository,
        IUnitOfWork unitOfWork,
        ITelegramSender telegramSender,
        IOptions<EmailSettings> emailSettings,
        IOptionsMonitor<EmailChainOptions> chainOptions)
    {
        _healthService = healthService;
        _preselectService = preselectService;
        _emailMarketingService = emailMarketingService;
        _providerBreaker = providerBreaker;
        _pilotBreaker = pilotBreaker;
        _providerPolicy = providerPolicy;
        _queueRepository = queueRepository;
        _campaignRepository = campaignRepository;
        _unitOfWork = unitOfWork;
        _telegramSender = telegramSender;
        _emailSettings = emailSettings.Value;
        _chainOptions = chainOptions;
    }

    /// <summary>
    /// Returns send counts, limits and health status for each email provider.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _healthService.GetHealthAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns up to maxPerProvider * 3 leads pre-assigned to Brevo/Mailjet/Resend
    /// sorted by segment (Hot first), score and cooldown recency.
    /// </summary>
    [HttpPost("smart-preselect")]
    public async Task<IActionResult> SmartPreselect(
        [FromBody] SmartPreselectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _preselectService.PreselecAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Queues email items for an existing campaign using the provider assignment
    /// from the smart preselection step (each lead already has AssignedProvider set).
    /// </summary>
    [HttpPost("queue-with-assignment")]
    public async Task<IActionResult> QueueWithAssignment(
        [FromBody] QueueWithAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.QueueWithSmartAssignmentAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Estado dos circuit breakers: global (piloto) + por provider, e a lista de
    /// providers habilitados/desabilitados.
    /// </summary>
    [HttpGet("breaker-status")]
    public IActionResult GetBreakerStatus()
    {
        var providerStates = _providerBreaker.GetStates();

        return Ok(new
        {
            pilot = new
            {
                isOpen = _pilotBreaker.IsOpen,
                reason = _pilotBreaker.Reason,
                currentErrorRate = _pilotBreaker.CurrentErrorRate,
                webhookFailureCount = _pilotBreaker.WebhookFailureCount
            },
            providers = providerStates.Values.OrderBy(s => s.Provider).Select(s => new
            {
                provider = s.Provider,
                isOpen = s.IsOpen,
                isHalfOpen = s.IsHalfOpen,
                reason = s.Reason,
                openedAtUtc = s.OpenedAtUtc,
                errorRatePercent = s.ErrorRatePercent
            }),
            enabledProviders = _providerPolicy.EnabledProviders.Select(p => p.ToString())
        });
    }

    /// <summary>
    /// Fecha manualmente o breaker de um provider específico do dispatch unificado.
    /// (O breaker global do piloto tem o próprio reset em /email-campaigns/pilot/reset.)
    /// </summary>
    [HttpPost("breaker/reset")]
    [Authorize(Roles = "Admin")]
    public IActionResult ResetProviderBreaker([FromQuery] string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return BadRequest(new { error = "Validation", message = "Informe o provider (ex: ?provider=brevo)." });
        }

        _providerBreaker.Reset(provider.Trim().ToLowerInvariant());
        return Ok(new { reset = provider.Trim().ToLowerInvariant() });
    }

    /// <summary>
    /// Visão consolidada de saúde do módulo de email: fila por status, DLQ, breakers,
    /// envio por provider vs limite diário e configuração de alertas/fallback.
    /// Alimenta o dashboard de saúde do CRM.
    /// </summary>
    [HttpGet("ops-summary")]
    public async Task<IActionResult> GetOpsSummary(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var startOfDay = now.Date;
        var startOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        var queued = await _queueRepository.CountByStatusAsync(EmailQueueStatus.Queued, cancellationToken);
        var processing = await _queueRepository.CountByStatusAsync(EmailQueueStatus.Processing, cancellationToken);
        var failed = await _queueRepository.CountByStatusAsync(EmailQueueStatus.Failed, cancellationToken);
        var deadLettered = await _queueRepository.CountByStatusAsync(EmailQueueStatus.DeadLettered, cancellationToken);
        var sentToday = await _queueRepository.CountSentSinceAsync(startOfDay, cancellationToken);
        var sentLastHour = await _queueRepository.CountSentSinceAsync(startOfHour, cancellationToken);

        var breakerStates = _providerBreaker.GetStates();
        var chainLimits = _chainOptions.CurrentValue.ProviderDailyLimits;

        var providers = new List<object>();
        foreach (EmailProvider provider in Enum.GetValues<EmailProvider>())
        {
            var key = EmailProviderPolicy.KeyOf(provider);
            var enabled = _providerPolicy.IsEnabled(provider);
            var providerSentToday = await _queueRepository.CountSentByProviderSinceAsync(provider, startOfDay, cancellationToken);
            var queuedForProvider = await _queueRepository.CountQueuedByProviderAsync(provider, cancellationToken);
            breakerStates.TryGetValue(key, out var breaker);
            chainLimits.TryGetValue(key, out var dailyLimit);

            providers.Add(new
            {
                provider = provider.ToString(),
                key,
                enabled,
                sentToday = providerSentToday,
                queued = queuedForProvider,
                dailyLimit = dailyLimit > 0 ? dailyLimit : (int?)null,
                breakerOpen = breaker?.IsOpen ?? false,
                breakerHalfOpen = breaker?.IsHalfOpen ?? false,
                breakerReason = breaker?.Reason
            });
        }

        return Ok(new
        {
            generatedAtUtc = now,
            queue = new { queued, processing, failed, deadLettered, sentToday, sentLastHour },
            limits = new { daily = _emailSettings.DailyLimit, hourly = _emailSettings.HourlyLimit },
            pilot = new { isOpen = _pilotBreaker.IsOpen, reason = _pilotBreaker.Reason },
            providers,
            ops = new
            {
                telegramConfigured = _telegramSender.IsConfigured,
                opsAlertsEnabled = _emailSettings.OpsAlertsEnabled,
                inCycleFallbackEnabled = _emailSettings.InCycleFallbackEnabled,
                maxFallbackProvidersPerItem = _emailSettings.MaxFallbackProvidersPerItem,
                sandboxRedirectTo = string.IsNullOrWhiteSpace(_emailSettings.SandboxRedirectTo) ? null : _emailSettings.SandboxRedirectTo
            }
        });
    }

    /// <summary>
    /// Dead-letter queue: itens que esgotaram todas as tentativas (inclusive fallback).
    /// </summary>
    [HttpGet("dead-letter")]
    public async Task<IActionResult> GetDeadLetter(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _queueRepository.GetDeadLetteredPagedAsync(page, pageSize, cancellationToken);

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            items = items.Select(item => new
            {
                id = item.Id,
                recipientEmail = item.RecipientEmail,
                recipientName = item.RecipientName,
                subject = item.Subject,
                assignedProvider = item.AssignedProvider.ToString(),
                attemptCount = item.AttemptCount,
                lastError = item.LastError,
                campaignId = item.CampaignId,
                deadLetteredAtUtc = item.UpdatedAt,
                createdAtUtc = item.CreatedAt
            })
        });
    }

    /// <summary>
    /// Requeue manual de um item da DLQ: zera as tentativas, reatribui para um provider
    /// habilitado e volta para a fila imediatamente. Corrige o FailedCount da campanha.
    /// </summary>
    [HttpPost("dead-letter/{id:guid}/requeue")]
    public async Task<IActionResult> RequeueDeadLetter([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _queueRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(new { error = "NotFound", message = "Item não encontrado." });
        }

        if (item.Status != EmailQueueStatus.DeadLettered)
        {
            return BadRequest(new { error = "InvalidState", message = $"Item está em '{item.Status}', não em DeadLettered." });
        }

        var next = _providerPolicy.NextEnabledAfter(item.AssignedProvider);
        if (next is null)
        {
            return BadRequest(new { error = "NoProvider", message = "Nenhum provider de email habilitado." });
        }

        item.ReassignProvider(next.Value);
        item.RestoreFromDeadLetter(DateTime.UtcNow);
        await _queueRepository.UpdateAsync(item, cancellationToken);

        // O item contou como Failed na campanha quando morreu; voltando à fila o Failed
        // não é mais terminal — mesmo ajuste feito pelo retry automático.
        if (item.CampaignId.HasValue)
        {
            await _campaignRepository.DecrementFailedAsync(item.CampaignId.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            requeued = item.Id,
            provider = item.AssignedProvider.ToString(),
            scheduledAtUtc = item.ScheduledAt
        });
    }
}
