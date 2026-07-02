using System.Text.Json;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Um ciclo completo de despacho da fila de campanhas. Extraído do BackgroundService
/// para ser testável e para concentrar as regras de resiliência:
///  - recovery de itens presos em Processing (crash entre envio e save);
///  - breaker POR PROVIDER (metade-aberto) além do breaker global do piloto;
///  - parada imediata do ciclo quando o breaker global abre;
///  - limites por provider (EmailChain) contados do banco — sobrevivem a recycle;
///  - retry com reassinalação de provider + correção do FailedCount;
///  - reassinalação de itens presos em providers desabilitados;
///  - sandbox mode (redireciona destinatários fora de produção).
/// </summary>
public class EmailQueueCycleProcessor
{
    public const int MaxRetryAttempts = 3;
    private const int RetryLookbackDays = 7;
    private const int StaleRecoveryBatchSize = 200;
    private static readonly TimeSpan StaleProcessingAfter = TimeSpan.FromMinutes(30);

    private readonly IEmailQueueRepository _repository;
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailTemplateEngine _templateEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPilotCircuitBreaker _pilotBreaker;
    private readonly IProviderCircuitBreaker _providerBreaker;
    private readonly IEmailProviderPolicy _providerPolicy;
    private readonly IUnsubscribeLinkBuilder _linkBuilder;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailSettings _settings;
    private readonly IOptionsMonitor<EmailChainOptions> _chainOptions;
    private readonly ILogger<EmailQueueCycleProcessor> _logger;

    public EmailQueueCycleProcessor(
        IEmailQueueRepository repository,
        IEmailCampaignRepository campaignRepository,
        ICustomerRepository customerRepository,
        IEmailTemplateEngine templateEngine,
        IUnitOfWork unitOfWork,
        IPilotCircuitBreaker pilotBreaker,
        IProviderCircuitBreaker providerBreaker,
        IEmailProviderPolicy providerPolicy,
        IUnsubscribeLinkBuilder linkBuilder,
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        IServiceProvider serviceProvider,
        IOptions<EmailSettings> settings,
        IOptionsMonitor<EmailChainOptions> chainOptions,
        ILogger<EmailQueueCycleProcessor> logger)
    {
        _repository = repository;
        _campaignRepository = campaignRepository;
        _customerRepository = customerRepository;
        _templateEngine = templateEngine;
        _unitOfWork = unitOfWork;
        _pilotBreaker = pilotBreaker;
        _providerBreaker = providerBreaker;
        _providerPolicy = providerPolicy;
        _linkBuilder = linkBuilder;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _chainOptions = chainOptions;
        _logger = logger;
    }

    public async Task<int> ProcessOnceAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await RecoverStaleProcessingAsync(now, cancellationToken);
        await ReassignFromDisabledProvidersAsync(now, cancellationToken);

        var enabledProviders = _providerPolicy.EnabledProviders;
        if (enabledProviders.Count == 0)
        {
            _logger.LogWarning("Nenhum provider de email habilitado — ciclo pulado.");
            return 0;
        }

        if (_pilotBreaker.IsOpen)
        {
            await LogBlockedCampaignsAsync(enabledProviders, now, cancellationToken);
            return 0;
        }

        // Limites globais (contados do banco — sobrevivem a recycle)
        var dailyLimit = _settings.DailyLimit > 0 ? _settings.DailyLimit : int.MaxValue;
        var hourlyLimit = _settings.HourlyLimit > 0 ? _settings.HourlyLimit : int.MaxValue;
        var startOfDay = now.Date;
        var startOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        var sentToday = await _repository.CountSentSinceAsync(startOfDay, cancellationToken);
        var sentThisHour = await _repository.CountSentSinceAsync(startOfHour, cancellationToken);
        var systemRemaining = Math.Min(Math.Max(0, dailyLimit - sentToday), Math.Max(0, hourlyLimit - sentThisHour));

        if (systemRemaining == 0)
        {
            _logger.LogInformation(
                "Limite atingido (hoje: {Today}/{Daily}, hora: {Hour}/{Hourly}). Saltando ciclo.",
                sentToday, dailyLimit, sentThisHour, hourlyLimit);
            await RequeuePendingRetriesAsync(now, cancellationToken);
            return 0;
        }

        var configuredPerProvider = _settings.PerProviderBatchSize <= 0 ? 20 : _settings.PerProviderBatchSize;
        var perProvider = Math.Min(
            configuredPerProvider,
            (int)Math.Ceiling((double)systemRemaining / enabledProviders.Count));

        _logger.LogInformation(
            "Ciclo iniciado. perProvider={PerProvider} (hoje: {Today}/{Daily}, hora: {Hour}/{Hourly})",
            perProvider, sentToday, dailyLimit, sentThisHour, hourlyLimit);

        var totalProcessed = 0;
        var chainLimits = _chainOptions.CurrentValue.ProviderDailyLimits;

        foreach (var provider in enabledProviders)
        {
            var serviceKey = EmailProviderPolicy.KeyOf(provider);

            if (_providerBreaker.IsOpen(serviceKey))
            {
                _logger.LogWarning("[{Provider}] breaker do provider aberto — pulando neste ciclo.", provider);
                continue;
            }

            // Limite diário POR PROVIDER (EmailChain) — antes o worker só tinha limite
            // global e podia estourar o teto do free tier de um provider isolado.
            var take = perProvider;
            if (chainLimits.TryGetValue(serviceKey, out var providerDailyLimit) && providerDailyLimit > 0)
            {
                var providerSentToday = await _repository.CountSentByProviderSinceAsync(provider, startOfDay, cancellationToken);
                var providerRemaining = providerDailyLimit - providerSentToday;
                if (providerRemaining <= 0)
                {
                    _logger.LogInformation(
                        "[{Provider}] limite diário do provider atingido ({Sent}/{Limit}) — pulando.",
                        provider, providerSentToday, providerDailyLimit);
                    continue;
                }

                take = Math.Min(take, providerRemaining);
            }

            var pendingItems = await _repository.GetPendingBatchByProviderAsync(provider, now, take, cancellationToken);
            if (pendingItems.Count == 0)
            {
                continue;
            }

            var emailSender = _serviceProvider.GetRequiredKeyedService<IEmailSender>(serviceKey);

            await StartScheduledCampaignsAsync(pendingItems, cancellationToken);

            foreach (var item in pendingItems)
            {
                var outcome = await ProcessItemAsync(item, emailSender, serviceKey, cancellationToken);
                totalProcessed++;

                if (outcome == ItemOutcome.PilotBreakerOpened)
                {
                    // Antes o ciclo continuava despachando o resto do lote (e os outros
                    // providers) mesmo com o breaker global aberto — o bloqueio só valia
                    // no ciclo seguinte. Agora para na hora.
                    _logger.LogWarning(
                        "Circuit breaker global abriu no meio do ciclo — interrompendo despacho imediatamente. Motivo: {Reason}",
                        _pilotBreaker.Reason);
                    return totalProcessed;
                }

                if (outcome == ItemOutcome.ProviderBreakerOpened)
                {
                    _logger.LogWarning(
                        "[{Provider}] breaker do provider abriu no meio do lote — passando para o próximo provider.",
                        provider);
                    break;
                }
            }

            _logger.LogInformation("[{Provider}] lote processado.", provider);
        }

        if (totalProcessed > 0)
        {
            _logger.LogInformation("Ciclo concluído. Total: {Total}", totalProcessed);
        }

        await RequeuePendingRetriesAsync(now, cancellationToken);
        return totalProcessed;
    }

    private enum ItemOutcome
    {
        Sent,
        Failed,
        ProviderBreakerOpened,
        PilotBreakerOpened
    }

    private async Task<ItemOutcome> ProcessItemAsync(
        EmailQueueItem item,
        IEmailSender emailSender,
        string serviceKey,
        CancellationToken cancellationToken)
    {
        item.MarkProcessing();
        await _repository.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Customer? customer = null;
        if (item.CustomerId.HasValue)
        {
            customer = await _customerRepository.GetByIdAsync(item.CustomerId.Value, cancellationToken);
        }

        var unsubscribeUrl = _linkBuilder.BuildUnsubscribeUrl(item.UserId, item.RecipientEmail);
        var variables = EmailMarketingService.BuildRecipientTemplateVariables(
            customer, item, unsubscribeUrl, _linkBuilder.DefaultCtaUrl);

        var renderedSubject = _templateEngine.Render(item.Subject, variables);
        var renderedHtmlBody = _templateEngine.Render(item.HtmlBody, variables);

        var recipientEmail = item.RecipientEmail;
        var recipientName = item.RecipientName;

        // Sandbox: fora de produção nada sai para leads reais.
        if (!string.IsNullOrWhiteSpace(_settings.SandboxRedirectTo))
        {
            renderedSubject = $"[SANDBOX p/ {recipientEmail}] {renderedSubject}";
            recipientEmail = _settings.SandboxRedirectTo;
            recipientName = "Sandbox";
            _logger.LogInformation(
                "Sandbox ativo — item {ItemId} redirecionado de {Original} para {Sandbox}",
                item.Id, item.RecipientEmail, _settings.SandboxRedirectTo);
        }

        var message = new EmailSendMessage
        {
            RecipientName = recipientName,
            RecipientEmail = recipientEmail,
            Subject = renderedSubject,
            HtmlBody = renderedHtmlBody,
            Attachments = ParseAttachments(item.AttachmentsJson),
            Tags = item.CampaignId.HasValue ? [item.CampaignId.Value.ToString()] : null
        };

        var sendResult = await emailSender.SendAsync(message, cancellationToken);

        if (sendResult.Success)
        {
            item.MarkSent(sendResult.ProviderMessageId);
            _providerBreaker.RecordSuccess(serviceKey);
            if (item.CampaignId.HasValue)
            {
                await _campaignRepository.IncrementSentAsync(item.CampaignId.Value, cancellationToken);
                _pilotBreaker.RecordSuccess();
            }

            await _repository.UpdateAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ItemOutcome.Sent;
        }

        var error = sendResult.ErrorMessage ?? "Falha desconhecida ao enviar e-mail.";
        item.MarkFailed(error);
        _providerBreaker.RecordFailure(serviceKey, error);

        var pilotOpened = false;
        if (item.CampaignId.HasValue)
        {
            await _campaignRepository.IncrementFailedAsync(item.CampaignId.Value, cancellationToken);

            var wasClosed = !_pilotBreaker.IsOpen;
            _pilotBreaker.RecordFailure(error);
            pilotOpened = wasClosed && _pilotBreaker.IsOpen;

            if (pilotOpened)
            {
                await LogPilotEventAsync(
                    "PilotCircuitBreakerOpened",
                    "Failed",
                    item.CampaignId.Value,
                    1,
                    false,
                    $"Circuit Breaker aberto devido a erro de envio: {error}",
                    item.UserId,
                    cancellationToken);
            }
        }

        await _repository.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (pilotOpened)
        {
            return ItemOutcome.PilotBreakerOpened;
        }

        return _providerBreaker.IsOpen(serviceKey)
            ? ItemOutcome.ProviderBreakerOpened
            : ItemOutcome.Failed;
    }

    /// <summary>
    /// Itens presos em Processing (crash entre MarkProcessing e o save final) eram
    /// invisíveis para sempre: nenhuma query buscava esse status — email perdido em
    /// silêncio. Volta para a fila (ou Failed se esgotou tentativas).
    /// </summary>
    private async Task RecoverStaleProcessingAsync(DateTime now, CancellationToken cancellationToken)
    {
        var stale = await _repository.GetStaleProcessingAsync(
            now - StaleProcessingAfter, StaleRecoveryBatchSize, cancellationToken);

        if (stale.Count == 0) return;

        foreach (var item in stale)
        {
            if (item.AttemptCount >= MaxRetryAttempts)
            {
                item.MarkFailed("Item preso em Processing (provável crash durante envio) — máximo de tentativas atingido.");
                if (item.CampaignId.HasValue)
                {
                    await _campaignRepository.IncrementFailedAsync(item.CampaignId.Value, cancellationToken);
                }
            }
            else
            {
                // O envio anterior PODE ter sido aceito pelo provider antes do crash —
                // requeue implica risco de duplicidade, mas perder o email é pior.
                _logger.LogWarning(
                    "Item {ItemId} preso em Processing desde {Since:u} — reenfileirado (tentativa {Attempt}/{Max}; possível duplicidade se o provider aceitou o envio anterior).",
                    item.Id, item.ProcessingStartedAt, item.AttemptCount, MaxRetryAttempts);
                item.Requeue(now);
            }

            await _repository.UpdateAsync(item, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Recovery: {Count} item(s) resgatado(s) de Processing órfão.", stale.Count);
    }

    /// <summary>
    /// Itens enfileirados para providers desabilitados nunca seriam processados
    /// (o loop só percorre habilitados) — realoca para providers vivos.
    /// </summary>
    private async Task ReassignFromDisabledProvidersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var enabled = _providerPolicy.EnabledProviders;
        if (enabled.Count == 0) return;

        var reassigned = 0;
        foreach (EmailProvider provider in Enum.GetValues<EmailProvider>())
        {
            if (_providerPolicy.IsEnabled(provider)) continue;

            var orphans = await _repository.GetPendingBatchByProviderAsync(
                provider, now, StaleRecoveryBatchSize, cancellationToken);

            foreach (var item in orphans)
            {
                var target = enabled[reassigned % enabled.Count];
                item.ReassignProvider(target);
                await _repository.UpdateAsync(item, cancellationToken);
                reassigned++;
            }
        }

        if (reassigned > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reassinalação: {Count} item(s) movido(s) de providers desabilitados.", reassigned);
        }
    }

    private async Task StartScheduledCampaignsAsync(
        IReadOnlyList<EmailQueueItem> pendingItems,
        CancellationToken cancellationToken)
    {
        var campaignIdsToStart = pendingItems
            .Where(i => i.CampaignId.HasValue)
            .Select(i => i.CampaignId!.Value)
            .Distinct()
            .ToList();

        foreach (var campaignId in campaignIdsToStart)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);
            if (campaign is { Status: EmailCampaignStatus.Scheduled })
            {
                campaign.StartProcessing();
                await _campaignRepository.UpdateAsync(campaign, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Campaign {CampaignId} transitioned from Scheduled to Processing", campaignId);
            }
        }
    }

    private async Task LogBlockedCampaignsAsync(
        IReadOnlyList<EmailProvider> enabledProviders,
        DateTime now,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Circuit breaker is OPEN. Reason: {Reason}. Skipping email dispatch.", _pilotBreaker.Reason);

        // Antes só os itens do Brevo eram considerados no log de bloqueio — auditoria
        // enganosa. Agora varre os pendentes de todos os providers habilitados.
        var allPending = new List<EmailQueueItem>();
        foreach (var provider in enabledProviders)
        {
            var batch = await _repository.GetPendingBatchByProviderAsync(provider, now, 100, cancellationToken);
            allPending.AddRange(batch);
        }

        var campaignIdsPending = allPending
            .Where(i => i.CampaignId.HasValue)
            .Select(i => i.CampaignId!.Value)
            .Distinct()
            .ToList();

        foreach (var campaignId in campaignIdsPending)
        {
            var firstItem = allPending.First(i => i.CampaignId == campaignId);
            await LogPilotEventAsync(
                "PilotRealSendBlocked",
                "Blocked",
                campaignId,
                allPending.Count(i => i.CampaignId == campaignId),
                false,
                $"Circuit Breaker aberto: {_pilotBreaker.Reason}",
                firstItem.UserId,
                cancellationToken);
        }
    }

    private async Task RequeuePendingRetriesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var cutoff = now.AddDays(-RetryLookbackDays);
        var failedItems = await _repository.GetFailedForRetryAsync(MaxRetryAttempts, cutoff, cancellationToken);

        if (failedItems.Count == 0) return;

        foreach (var item in failedItems)
        {
            // Exponential backoff: 2^AttemptCount * 15 min
            var delayMinutes = Math.Pow(2, item.AttemptCount) * 15;
            var retryAt = now.AddMinutes(delayMinutes);

            // Reassinalação: insistir no mesmo provider que acabou de falhar desperdiça
            // as 3 tentativas — roda para o próximo habilitado.
            var next = _providerPolicy.NextEnabledAfter(item.AssignedProvider);
            if (next.HasValue && next.Value != item.AssignedProvider)
            {
                item.ReassignProvider(next.Value);
            }

            item.Requeue(retryAt);
            await _repository.UpdateAsync(item, cancellationToken);

            // O Failed deste item não é terminal (vai tentar de novo) — sem o decremento,
            // o destinatário contava como Failed E Sent e Sent+Failed estourava o total.
            if (item.CampaignId.HasValue)
            {
                await _campaignRepository.DecrementFailedAsync(item.CampaignId.Value, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Retry: {Count} item(s) reagendado(s).", failedItems.Count);
    }

    private static List<EmailSendAttachment> ParseAttachments(string? attachmentsJson)
    {
        if (string.IsNullOrWhiteSpace(attachmentsJson))
        {
            return [];
        }

        try
        {
            var attachments = JsonSerializer.Deserialize<List<EmailAttachmentRequestDto>>(attachmentsJson);
            if (attachments is null)
            {
                return [];
            }

            return attachments.Select(attachment => new EmailSendAttachment
            {
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                Base64Content = attachment.Base64Content
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task LogPilotEventAsync(
        string action,
        string result,
        Guid campaignId,
        int leadCount,
        bool dryRun,
        string? blockingReasons,
        Guid userId,
        CancellationToken cancellationToken)
    {
        string userEmail = "sistema";
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            userEmail = user.Email;
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
            TimestampUtc = DateTime.UtcNow
        };

        var entry = AuditLogEntry.Create(
            userId,
            AuditAction.Custom,
            "PilotCampaign",
            campaignId.ToString(),
            $"Pilot campaign event: {action} ({result})",
            newValues: JsonSerializer.Serialize(details)
        );

        await _auditLogRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
