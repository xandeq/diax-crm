using System.Text.Json;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
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
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = DateTime.UtcNow;
        var sentInLastHour = await repository.CountSentSinceAsync(now.AddHours(-1), cancellationToken);
        var sentInLastDay = await repository.CountSentSinceAsync(now.AddDays(-1), cancellationToken);

        var hourlyLimit = _settings.HourlyLimit <= 0 ? 50 : _settings.HourlyLimit;
        var dailyLimit = _settings.DailyLimit <= 0 ? 250 : _settings.DailyLimit;

        var availableHour = Math.Max(0, hourlyLimit - sentInLastHour);
        var availableDay = Math.Max(0, dailyLimit - sentInLastDay);
        var allowedToSend = Math.Min(availableHour, availableDay);
        var batchSize = _settings.BatchSize <= 0 ? 50 : _settings.BatchSize;
        var take = Math.Min(batchSize, allowedToSend);

        if (take <= 0)
        {
            _logger.LogInformation(
                "Limite de envio atingido. Última hora: {HourCount}/{HourLimit}, último dia: {DayCount}/{DayLimit}",
                sentInLastHour,
                hourlyLimit,
                sentInLastDay,
                dailyLimit);
            return;
        }

        var pendingItems = await repository.GetPendingBatchAsync(now, take, cancellationToken);

        if (pendingItems.Count == 0)
        {
            return;
        }

        // Transition any Scheduled campaigns to Processing now that their items are being sent
        var campaignIdsToStart = pendingItems
            .Where(i => i.CampaignId.HasValue)
            .Select(i => i.CampaignId!.Value)
            .Distinct()
            .ToList();

        foreach (var campaignId in campaignIdsToStart)
        {
            var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
            if (campaign is { Status: Domain.EmailMarketing.Enums.EmailCampaignStatus.Scheduled })
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
                Attachments = ParseAttachments(item.AttachmentsJson)
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

        _logger.LogInformation("Processamento de fila finalizado. Itens processados: {Count}", pendingItems.Count);
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
