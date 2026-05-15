using System.Text.Json;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class EmailQueueProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailQueueProcessorWorker> _logger;

    private const int MaxRetryAttempts = 3;
    private const int RetryLookbackDays = 7;

    private static readonly (EmailProvider Provider, string ServiceKey)[] ProviderMap =
    [
        (EmailProvider.Brevo,        "brevo"),
        (EmailProvider.Mailjet,      "mailjet"),
        (EmailProvider.Resend,       "resend"),
        (EmailProvider.SendGrid,     "sendgrid"),
        (EmailProvider.MailerSend,   "mailersend"),
        (EmailProvider.ElasticEmail, "elasticemail"),
    ];

    public EmailQueueProcessorWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailSettings> settings,
        ILogger<EmailQueueProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueProcessorWorker iniciado. Intervalo: {Interval} minuto(s)", _settings.DispatchIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar fila de e-mails.");
            }

            var delayMinutes = _settings.DispatchIntervalMinutes <= 0 ? 5 : _settings.DispatchIntervalMinutes;
            await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
        var campaignRepository = scope.ServiceProvider.GetRequiredService<IEmailCampaignRepository>();
        var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<IEmailTemplateEngine>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var configuredPerProvider = _settings.PerProviderBatchSize <= 0 ? 20 : _settings.PerProviderBatchSize;

        // Enforce daily and hourly limits
        var dailyLimit = _settings.DailyLimit > 0 ? _settings.DailyLimit : int.MaxValue;
        var hourlyLimit = _settings.HourlyLimit > 0 ? _settings.HourlyLimit : int.MaxValue;
        var startOfDay = now.Date;
        var startOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        var sentToday = await repository.CountSentSinceAsync(startOfDay, cancellationToken);
        var sentThisHour = await repository.CountSentSinceAsync(startOfHour, cancellationToken);
        var dailyRemaining = Math.Max(0, dailyLimit - sentToday);
        var hourlyRemaining = Math.Max(0, hourlyLimit - sentThisHour);
        var systemRemaining = Math.Min(dailyRemaining, hourlyRemaining);

        if (systemRemaining == 0)
        {
            _logger.LogInformation(
                "Limite atingido (hoje: {Today}/{Daily}, hora: {Hour}/{Hourly}). Saltando ciclo.",
                sentToday, dailyLimit, sentThisHour, hourlyLimit);
            await RequeuePendingRetriesAsync(repository, unitOfWork, now, cancellationToken);
            return;
        }

        var numProviders = ProviderMap.Length;
        var perProvider = Math.Min(
            configuredPerProvider,
            (int)Math.Ceiling((double)systemRemaining / numProviders));

        _logger.LogInformation(
            "Ciclo iniciado. perProvider={PerProvider} (hoje: {Today}/{Daily}, hora: {Hour}/{Hourly})",
            perProvider, sentToday, dailyLimit, sentThisHour, hourlyLimit);

        var totalProcessed = 0;

        foreach (var (provider, serviceKey) in ProviderMap)
        {
            var pendingItems = await repository.GetPendingBatchByProviderAsync(provider, now, perProvider, cancellationToken);

            if (pendingItems.Count == 0)
            {
                continue;
            }

            var emailSender = scope.ServiceProvider.GetRequiredKeyedService<IEmailSender>(serviceKey);

            // Transition any Scheduled campaigns to Processing
            var campaignIdsToStart = pendingItems
                .Where(i => i.CampaignId.HasValue)
                .Select(i => i.CampaignId!.Value)
                .Distinct()
                .ToList();

            foreach (var campaignId in campaignIdsToStart)
            {
                var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
                if (campaign is { Status: EmailCampaignStatus.Scheduled })
                {
                    campaign.StartProcessing();
                    await campaignRepository.UpdateAsync(campaign, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Campaign {CampaignId} transitioned from Scheduled to Processing", campaignId);
                }
            }

            foreach (var item in pendingItems)
            {
                item.MarkProcessing();
                await repository.UpdateAsync(item, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                Customer? customer = null;
                if (item.CustomerId.HasValue)
                {
                    customer = await customerRepository.GetByIdAsync(item.CustomerId.Value, cancellationToken);
                }

                var variables = EmailMarketingService.BuildRecipientTemplateVariables(customer, item);
                var renderedSubject = templateEngine.Render(item.Subject, variables);
                var renderedHtmlBody = templateEngine.Render(item.HtmlBody, variables);

                var message = new EmailSendMessage
                {
                    RecipientName = item.RecipientName,
                    RecipientEmail = item.RecipientEmail,
                    Subject = renderedSubject,
                    HtmlBody = renderedHtmlBody,
                    Attachments = ParseAttachments(item.AttachmentsJson),
                    Tags = item.CampaignId.HasValue ? [item.CampaignId.Value.ToString()] : null
                };

                var sendResult = await emailSender.SendAsync(message, cancellationToken);

                if (sendResult.Success)
                {
                    item.MarkSent(sendResult.ProviderMessageId);
                    if (item.CampaignId.HasValue)
                    {
                        await campaignRepository.IncrementSentAsync(item.CampaignId.Value, cancellationToken);
                    }
                }
                else
                {
                    item.MarkFailed(sendResult.ErrorMessage ?? "Falha desconhecida ao enviar e-mail.");
                    if (item.CampaignId.HasValue)
                    {
                        await campaignRepository.IncrementFailedAsync(item.CampaignId.Value, cancellationToken);
                    }
                }

                await repository.UpdateAsync(item, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            totalProcessed += pendingItems.Count;
            _logger.LogInformation("[{Provider}] {Count} emails processados.", provider, pendingItems.Count);
        }

        if (totalProcessed > 0)
        {
            _logger.LogInformation("Ciclo concluído. Total: {Total}", totalProcessed);
        }

        await RequeuePendingRetriesAsync(repository, unitOfWork, now, cancellationToken);
    }

    private async Task RequeuePendingRetriesAsync(
        IEmailQueueRepository repository,
        IUnitOfWork unitOfWork,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var cutoff = now.AddDays(-RetryLookbackDays);
        var failedItems = await repository.GetFailedForRetryAsync(MaxRetryAttempts, cutoff, cancellationToken);

        if (failedItems.Count == 0) return;

        foreach (var item in failedItems)
        {
            // Exponential backoff: 2^AttemptCount * 15 min
            var delayMinutes = Math.Pow(2, item.AttemptCount) * 15;
            var retryAt = now.AddMinutes(delayMinutes);
            item.Requeue(retryAt);
            await repository.UpdateAsync(item, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
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
}
