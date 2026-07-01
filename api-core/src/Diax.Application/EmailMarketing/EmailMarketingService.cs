using System.Net.Mail;
using System.Text.Json;
using Diax.Application.Common;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Domain.Snippets;
using Diax.Shared.Results;
using Diax.Domain.Audit;

namespace Diax.Application.EmailMarketing;

public class EmailMarketingService : IApplicationService
{
    private const int MaxAttachmentBytes = 10 * 1024 * 1024;

    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISnippetRepository _snippetRepository;
    private readonly IEmailTemplateEngine _emailTemplateEngine;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IPilotCircuitBreaker _circuitBreaker;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnsubscribeLinkBuilder _linkBuilder;
    private readonly IEmailProviderPolicy _providerPolicy;

    public EmailMarketingService(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        ISnippetRepository snippetRepository,
        IEmailTemplateEngine emailTemplateEngine,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IEmailSender emailSender,
        IEmailSuppressionRepository suppressionRepository,
        IPilotCircuitBreaker circuitBreaker,
        IAuditLogRepository auditLogRepository,
        IUnsubscribeLinkBuilder linkBuilder,
        IEmailProviderPolicy providerPolicy)
    {
        _emailQueueRepository = emailQueueRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _customerRepository = customerRepository;
        _snippetRepository = snippetRepository;
        _emailTemplateEngine = emailTemplateEngine;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _emailSender = emailSender;
        _suppressionRepository = suppressionRepository;
        _circuitBreaker = circuitBreaker;
        _auditLogRepository = auditLogRepository;
        _linkBuilder = linkBuilder;
        _providerPolicy = providerPolicy;
    }

    public async Task<Result<PreviewCampaignResponse>> PreviewCampaignAsync(
        Guid campaignId,
        PreviewCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<PreviewCampaignResponse>(campaignResult.Error);
        }

        var campaign = campaignResult.Value;
        var templateResult = await ResolveCampaignTemplateSnapshotAsync(
            campaign,
            request.BodyHtmlOverride,
            request.SourceSnippetIdOverride,
            cancellationToken);

        if (templateResult.IsFailure)
        {
            return Result.Failure<PreviewCampaignResponse>(templateResult.Error);
        }

        var subjectTemplate = string.IsNullOrWhiteSpace(request.SubjectOverride)
            ? campaign.Subject
            : request.SubjectOverride.Trim();

        var variables = BuildTemplateVariables(
            request.MockData.FirstName,
            request.MockData.Email,
            request.MockData.Company,
            request.MockData.LeadStatus);

        var renderedSubject = _emailTemplateEngine.Render(subjectTemplate, variables);
        var renderedBody = _emailTemplateEngine.Render(templateResult.Value.TemplateBody, variables);

        return new PreviewCampaignResponse
        {
            CampaignId = campaign.Id,
            TemplateSource = templateResult.Value.Source,
            SubjectTemplate = subjectTemplate,
            BodyTemplate = templateResult.Value.TemplateBody,
            RenderedSubject = renderedSubject,
            RenderedBodyHtml = renderedBody,
            Variables = variables
        };
    }

    public async Task<Result> SendTestEmailAsync(
        Guid campaignId,
        SendTestEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure(campaignResult.Error);
        }

        var campaign = campaignResult.Value;

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId!.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User", _currentUserService.UserId.Value));
        }

        var userEmail = user.Email;
        var userName = userEmail;

        // Log requested event
        await LogPilotEventAsync("PilotSendTestRequested", "Success", campaignId, 1, true, null, cancellationToken);

        var templateResult = await ResolveCampaignTemplateSnapshotAsync(
            campaign,
            request.BodyHtmlOverride,
            null,
            cancellationToken);

        if (templateResult.IsFailure)
        {
            return Result.Failure(templateResult.Error);
        }

        var subjectTemplate = string.IsNullOrWhiteSpace(request.SubjectOverride)
            ? campaign.Subject
            : request.SubjectOverride.Trim();

        // Safe mock variables for test email
        var variables = BuildTemplateVariables("Teste", userEmail, "Empresa Teste", "Lead");
        variables["nome"] = "Teste";
        variables["empresa"] = "Empresa Teste";
        variables["site"] = "www.empresateste.com.br";
        variables["cidade"] = "São Paulo";
        variables["ferramenta_atual"] = "Pipedrive";
        variables["dor_principal"] = "falta de integração com o WhatsApp";
        variables["cta_link"] = _linkBuilder.DefaultCtaUrl;
        variables["unsubscribe_url"] = _linkBuilder.BuildUnsubscribeUrl(_currentUserService.UserId!.Value, userEmail);

        var renderedSubject = "[TESTE] " + _emailTemplateEngine.Render(subjectTemplate, variables);
        var renderedBody = _emailTemplateEngine.Render(templateResult.Value.TemplateBody, variables);

        var message = new EmailSendMessage
        {
            RecipientName = userName,
            RecipientEmail = userEmail,
            Subject = renderedSubject,
            HtmlBody = renderedBody,
            Tags = [campaignId.ToString()]
        };

        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        if (!sendResult.Success)
        {
            await LogPilotEventAsync("PilotSendTestCompleted", "Failed", campaignId, 1, true, sendResult.ErrorMessage ?? "Erro de envio", cancellationToken);
            return Result.Failure(
                Error.Validation("EmailSend", sendResult.ErrorMessage ?? "Falha ao enviar e-mail de teste."));
        }

        // Log completed event
        await LogPilotEventAsync("PilotSendTestCompleted", "Success", campaignId, 1, true, null, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<EmailCampaignResponse>> CreateCampaignAsync(
        CreateEmailCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Unauthorized("Usuário não autenticado para criar campanha."));
        }

        var snippet = await ResolveSnippetAsync(request.SourceSnippetId, cancellationToken);
        if (request.SourceSnippetId.HasValue && snippet is null)
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.NotFound("Snippet", request.SourceSnippetId.Value));
        }

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? snippet?.Title ?? string.Empty
            : request.Name.Trim();
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? snippet?.Title ?? string.Empty
            : request.Subject.Trim();
        var bodyHtml = string.IsNullOrWhiteSpace(request.BodyHtml)
            ? snippet?.Content ?? string.Empty
            : request.BodyHtml;

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Validation("Name", "Nome da campanha é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Validation("Subject", "Assunto da campanha é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(bodyHtml))
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Validation("BodyHtml", "Template HTML da campanha é obrigatório."));
        }

        var campaign = new EmailCampaign(
            _currentUserService.UserId.Value,
            name,
            subject,
            bodyHtml,
            request.SourceSnippetId);

        await _emailCampaignRepository.AddAsync(campaign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmailCampaignResponse.FromEntity(campaign);
    }

    public async Task<Result<EmailCampaignResponse>> UpdateCampaignAsync(
        Guid campaignId,
        UpdateEmailCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<EmailCampaignResponse>(campaignResult.Error);
        }

        var campaign = campaignResult.Value;
        if (campaign.Status != EmailCampaignStatus.Draft)
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Validation("Status", "Apenas campanhas em rascunho podem ser editadas."));
        }

        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Subject)
            || string.IsNullOrWhiteSpace(request.BodyHtml))
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Validation("Campaign", "Nome, assunto e template HTML são obrigatórios."));
        }

        if (request.SourceSnippetId.HasValue)
        {
            var snippet = await ResolveSnippetAsync(request.SourceSnippetId, cancellationToken);
            if (snippet is null)
            {
                return Result.Failure<EmailCampaignResponse>(
                    Error.NotFound("Snippet", request.SourceSnippetId.Value));
            }
        }

        campaign.UpdateContent(request.Name.Trim(), request.Subject.Trim(), request.BodyHtml);
        campaign.SetSourceSnippet(request.SourceSnippetId);
        await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmailCampaignResponse.FromEntity(campaign);
    }

    public async Task<Result<EmailCampaignResponse>> ScheduleCampaignAsync(
        Guid campaignId,
        ScheduleEmailCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<EmailCampaignResponse>(campaignResult.Error);
        }

        var campaign = campaignResult.Value;

        var contentReadiness = ValidateContentReadiness(campaign);
        if (contentReadiness.IsFailure)
        {
            return Result.Failure<EmailCampaignResponse>(contentReadiness.Error);
        }

        var scheduledAt = NormalizeSchedule(request.ScheduledAt);
        if (scheduledAt <= DateTime.UtcNow)
        {
            return Result.Failure<EmailCampaignResponse>(
                Error.Validation("ScheduledAt", "Informe uma data futura para agendamento."));
        }

        campaign.Schedule(scheduledAt);
        await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmailCampaignResponse.FromEntity(campaign);
    }

    public async Task<Result<EmailCampaignResponse>> GetCampaignByIdAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<EmailCampaignResponse>(campaignResult.Error);
        }

        return EmailCampaignResponse.FromEntity(campaignResult.Value);
    }

    public async Task<Result<PagedResponse<EmailCampaignResponse>>> GetCampaignsByCurrentUserAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<PagedResponse<EmailCampaignResponse>>(
                Error.Unauthorized("Usuário não autenticado para consultar campanhas."));
        }

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var (items, totalCount) = await _emailCampaignRepository.GetPagedByUserAsync(
            _currentUserService.UserId.Value,
            safePage,
            safePageSize,
            cancellationToken);

        var response = PagedResponse<EmailCampaignResponse>.Create(
            items.Select(EmailCampaignResponse.FromEntity),
            safePage,
            safePageSize,
            totalCount);

        return response;
    }

    public async Task<Result<QueueCampaignRecipientsResponse>> QueueCampaignRecipientsAsync(
        Guid campaignId,
        QueueCampaignRecipientsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerIds.Count == 0)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("CustomerIds", "Informe ao menos um cliente/lead para envio."));
        }

        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(campaignResult.Error);
        }

        var campaign = campaignResult.Value;

        if (_circuitBreaker.IsOpen)
        {
            await LogPilotEventAsync("PilotRealSendBlocked", "Blocked", campaignId, request.CustomerIds.Count, false, $"Circuit Breaker aberto: {_circuitBreaker.Reason}", cancellationToken);
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("CircuitBreaker", $"Envios reais bloqueados pelo Circuit Breaker: {_circuitBreaker.Reason}"));
        }

        var readinessResult = ValidateReadiness(campaign);
        await LogPilotEventAsync("PilotReadinessChecked", readinessResult.IsSuccess ? "Success" : "Failed", campaignId, request.CustomerIds.Count, false, readinessResult.IsSuccess ? null : readinessResult.Error.Message, cancellationToken);
        if (readinessResult.IsFailure)
        {
            await LogPilotEventAsync("PilotReadinessBlocked", "Blocked", campaignId, request.CustomerIds.Count, false, readinessResult.Error.Message, cancellationToken);
            await LogPilotEventAsync("PilotCampaignActivationBlocked", "Blocked", campaignId, request.CustomerIds.Count, false, readinessResult.Error.Message, cancellationToken);
            return Result.Failure<QueueCampaignRecipientsResponse>(readinessResult.Error);
        }

        if (campaign.Status is EmailCampaignStatus.Completed or EmailCampaignStatus.Cancelled)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Status", "Campanhas concluídas/canceladas não podem receber novos envios."));
        }

        var attachmentsJsonResult = BuildAttachmentsJson(request.Attachments);
        if (attachmentsJsonResult.IsFailure)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(attachmentsJsonResult.Error);
        }

        var recipients = await _customerRepository.FindAsync(
            customer => request.CustomerIds.Contains(customer.Id),
            cancellationToken);

        var templateResult = await ResolveCampaignTemplateSnapshotAsync(
            campaign,
            request.BodyHtmlOverride,
            null,
            cancellationToken);

        if (templateResult.IsFailure)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(templateResult.Error);
        }

        var scheduledAt = NormalizeSchedule(request.ScheduledAt ?? campaign.ScheduledAt);
        var queuedItems = new List<EmailQueueItem>();
        var skipped = new List<string>();
        var providerIndex = 0;

        // Rotação apenas entre providers HABILITADOS — providers sem credencial
        // (Email:DisabledProviders) não recebem itens que falhariam 100% das vezes.
        var enabledProviders = _providerPolicy.EnabledProviders;
        if (enabledProviders.Count == 0)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Providers", "Nenhum provider de email habilitado (Email:DisabledProviders)."));
        }

        var customerIds = recipients.Select(c => c.Id).ToList();
        var existingQueued = await _emailQueueRepository.FindAsync(
            q => q.CampaignId == campaign.Id && q.CustomerId.HasValue && customerIds.Contains(q.CustomerId.Value),
            cancellationToken);
        
        var existingQueuedCustomerIds = existingQueued
            .Where(q => q.CustomerId.HasValue)
            .Select(q => q.CustomerId!.Value)
            .ToHashSet();

        foreach (var customer in recipients)
        {
            if (!IsValidEmail(customer.Email))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (email inválido)");
                continue;
            }

            if (customer.EmailOptOut)
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (opt-out ativo)");
                continue;
            }

            if (await _suppressionRepository.IsSuppressedAsync(_currentUserService.UserId!.Value, customer.Email, cancellationToken))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (suprimido)");
                continue;
            }

            if (existingQueuedCustomerIds.Contains(customer.Id))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (já enfileirado ou enviado para esta campanha)");
                continue;
            }

            var provider = enabledProviders[providerIndex % enabledProviders.Count];
            providerIndex++;

            queuedItems.Add(new EmailQueueItem(
                _currentUserService.UserId!.Value,
                customer.Name,
                customer.Email,
                campaign.Subject,
                templateResult.Value.TemplateBody,
                scheduledAt,
                customer.Id,
                attachmentsJsonResult.Value,
                campaign.Id,
                provider));
        }

        if (queuedItems.Count == 0)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Recipients", "Nenhum destinatário válido encontrado para enfileirar."));
        }

        campaign.SetTotalRecipients(queuedItems.Count);
        if (scheduledAt > DateTime.UtcNow)
        {
            if (campaign.Status == EmailCampaignStatus.Draft)
            {
                campaign.Schedule(scheduledAt);
            }
        }
        else
        {
            campaign.StartProcessing();
        }

        await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
        await _emailQueueRepository.AddRangeAsync(queuedItems, cancellationToken);

        // Registra envio em cada customer para habilitar dedup cross-day via LastEmailSentAt/EmailSentCount
        var queuedCustomerIdsLegacy = queuedItems
            .Where(i => i.CustomerId.HasValue)
            .Select(i => i.CustomerId!.Value)
            .ToHashSet();
        foreach (var customer in recipients.Where(c => queuedCustomerIdsLegacy.Contains(c.Id)))
        {
            customer.RegisterEmailSent();
            await _customerRepository.UpdateAsync(customer, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new QueueCampaignRecipientsResponse
        {
            CampaignId = campaign.Id,
            RequestedCount = request.CustomerIds.Count,
            QueuedCount = queuedItems.Count,
            SkippedCount = skipped.Count,
            EffectiveScheduledAt = scheduledAt,
            SkippedRecipients = skipped
        };
    }

    public async Task<Result<EmailQueueItemResponse>> QueueSingleAsync(
        QueueSingleEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<EmailQueueItemResponse>(
                Error.Unauthorized("Usuário não autenticado para enfileirar e-mails."));
        }

        if (string.IsNullOrWhiteSpace(request.RecipientEmail) || !IsValidEmail(request.RecipientEmail))
        {
            return Result.Failure<EmailQueueItemResponse>(
                Error.Validation("RecipientEmail", "E-mail do destinatário é inválido."));
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return Result.Failure<EmailQueueItemResponse>(
                Error.Validation("Subject", "Assunto é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            return Result.Failure<EmailQueueItemResponse>(
                Error.Validation("HtmlBody", "Corpo HTML é obrigatório."));
        }

        var attachmentsJsonResult = BuildAttachmentsJson(request.Attachments);
        if (attachmentsJsonResult.IsFailure)
        {
            return Result.Failure<EmailQueueItemResponse>(attachmentsJsonResult.Error);
        }

        if (await _suppressionRepository.IsSuppressedAsync(_currentUserService.UserId.Value, request.RecipientEmail, cancellationToken))
        {
            return Result.Failure<EmailQueueItemResponse>(
                Error.Validation("RecipientEmail", "Destinatário está na lista de supressão (opt-out)."));
        }

        var scheduledAt = NormalizeSchedule(request.ScheduledAt);
        var queueItem = new EmailQueueItem(
            _currentUserService.UserId.Value,
            request.RecipientName,
            request.RecipientEmail,
            request.Subject,
            request.HtmlBody,
            scheduledAt,
            request.CustomerId,
            attachmentsJsonResult.Value);

        await _emailQueueRepository.AddAsync(queueItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmailQueueItemResponse.FromEntity(queueItem);
    }

    public async Task<Result<QueueBulkEmailResponse>> QueueBulkForCustomersAsync(
        QueueBulkEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<QueueBulkEmailResponse>(
                Error.Unauthorized("Usuário não autenticado para enfileirar e-mails."));
        }

        if (request.CustomerIds.Count == 0)
        {
            return Result.Failure<QueueBulkEmailResponse>(
                Error.Validation("CustomerIds", "Informe ao menos um cliente/lead para envio."));
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return Result.Failure<QueueBulkEmailResponse>(
                Error.Validation("Subject", "Assunto é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            return Result.Failure<QueueBulkEmailResponse>(
                Error.Validation("HtmlBody", "Corpo HTML é obrigatório."));
        }

        var attachmentsJsonResult = BuildAttachmentsJson(request.Attachments);
        if (attachmentsJsonResult.IsFailure)
        {
            return Result.Failure<QueueBulkEmailResponse>(attachmentsJsonResult.Error);
        }

        var recipients = await _customerRepository.FindAsync(
            customer => request.CustomerIds.Contains(customer.Id),
            cancellationToken);

        var scheduledAt = NormalizeSchedule(request.ScheduledAt);
        var queuedItems = new List<EmailQueueItem>();
        var skipped = new List<string>();

        foreach (var customer in recipients)
        {
            if (!IsValidEmail(customer.Email))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (email inválido)");
                continue;
            }

            if (customer.EmailOptOut)
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (opt-out ativo)");
                continue;
            }

            if (await _suppressionRepository.IsSuppressedAsync(_currentUserService.UserId.Value, customer.Email, cancellationToken))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (suprimido)");
                continue;
            }

            queuedItems.Add(new EmailQueueItem(
                _currentUserService.UserId.Value,
                customer.Name,
                customer.Email,
                request.Subject,
                request.HtmlBody,
                scheduledAt,
                customer.Id,
                attachmentsJsonResult.Value));
        }

        await _emailQueueRepository.AddRangeAsync(queuedItems, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new QueueBulkEmailResponse
        {
            RequestedCount = request.CustomerIds.Count,
            QueuedCount = queuedItems.Count,
            SkippedCount = skipped.Count,
            SkippedRecipients = skipped
        };

        return response;
    }

    public async Task<Result<PagedResponse<EmailQueueItemResponse>>> GetQueueByCurrentUserAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<PagedResponse<EmailQueueItemResponse>>(
                Error.Unauthorized("Usuário não autenticado para consultar fila."));
        }

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var (items, totalCount) = await _emailQueueRepository.GetPagedByUserAsync(
            _currentUserService.UserId.Value,
            safePage,
            safePageSize,
            cancellationToken);

        var response = PagedResponse<EmailQueueItemResponse>.Create(
            items.Select(EmailQueueItemResponse.FromEntity),
            safePage,
            safePageSize,
            totalCount);

        return response;
    }

    public async Task<Result<PagedResponse<EmailQueueItemResponse>>> GetCampaignRecipientsAsync(
        Guid campaignId,
        int page,
        int pageSize,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<PagedResponse<EmailQueueItemResponse>>(campaignResult.Error);
        }

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 50 : Math.Min(pageSize, 100);

        var (items, totalCount) = await _emailQueueRepository.GetPagedByCampaignIdFilteredAsync(
            campaignId,
            filter,
            safePage,
            safePageSize,
            cancellationToken);

        var response = PagedResponse<EmailQueueItemResponse>.Create(
            items.Select(EmailQueueItemResponse.FromEntity),
            safePage,
            safePageSize,
            totalCount);

        return response;
    }

    public async Task<Result<CampaignRecipientCustomerIdsResponse>> GetCampaignRecipientCustomerIdsAsync(
        Guid campaignId,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
            return Result.Failure<CampaignRecipientCustomerIdsResponse>(campaignResult.Error);

        var customerIds = await _emailQueueRepository.GetCustomerIdsByCampaignFilterAsync(
            campaignId, filter, cancellationToken);

        return new CampaignRecipientCustomerIdsResponse
        {
            CustomerIds = customerIds.Select(id => id.ToString()).ToList(),
            Count = customerIds.Count
        };
    }

    private static DateTime NormalizeSchedule(DateTime? scheduledAt)
    {
        var now = DateTime.UtcNow;
        if (!scheduledAt.HasValue)
        {
            return now;
        }

        var value = scheduledAt.Value;
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }
        else if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value < now ? now : value;
    }

    private async Task<Result<ResolvedCampaignTemplateSnapshot>> ResolveCampaignTemplateSnapshotAsync(
        EmailCampaign campaign,
        string? bodyHtmlOverride,
        Guid? sourceSnippetIdOverride,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(bodyHtmlOverride))
        {
            return new ResolvedCampaignTemplateSnapshot(bodyHtmlOverride, "body_override");
        }

        var snippetId = sourceSnippetIdOverride ?? campaign.SourceSnippetId;
        if (snippetId.HasValue)
        {
            var snippet = await ResolveSnippetAsync(snippetId, cancellationToken);
            if (snippet is null)
            {
                return Result.Failure<ResolvedCampaignTemplateSnapshot>(
                    Error.NotFound("Snippet", snippetId.Value));
            }

            return new ResolvedCampaignTemplateSnapshot(snippet.Content, "snippet_snapshot");
        }

        if (string.IsNullOrWhiteSpace(campaign.BodyHtml))
        {
            return Result.Failure<ResolvedCampaignTemplateSnapshot>(
                Error.Validation("BodyHtml", "Template HTML da campanha é obrigatório."));
        }

        return new ResolvedCampaignTemplateSnapshot(campaign.BodyHtml, "campaign_body");
    }

    private static Dictionary<string, string?> BuildTemplateVariables(
        string? firstName,
        string? email,
        string? company,
        string? leadStatus)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = firstName,
            ["Email"] = email,
            ["Company"] = company,
            ["CompanyName"] = company,
            ["LeadStatus"] = leadStatus
        };
    }

    private static string ExtractFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var split = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return split.Length == 0 ? string.Empty : split[0];
    }

    private async Task<Result<EmailCampaign>> GetCampaignOwnedByCurrentUserAsync(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<EmailCampaign>(
                Error.Unauthorized("Usuário não autenticado."));
        }

        var campaign = await _emailCampaignRepository.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.UserId != _currentUserService.UserId.Value)
        {
            return Result.Failure<EmailCampaign>(
                Error.NotFound("EmailCampaign", campaignId));
        }

        return campaign;
    }

    private async Task<Snippet?> ResolveSnippetAsync(Guid? snippetId, CancellationToken cancellationToken)
    {
        if (!snippetId.HasValue || _currentUserService.UserId is null)
        {
            return null;
        }

        return await _snippetRepository.GetByIdWithUserAsync(
            snippetId.Value,
            _currentUserService.UserId.Value,
            cancellationToken);
    }

    public static Dictionary<string, string?> BuildRecipientTemplateVariables(
        Customer? customer,
        EmailQueueItem queueItem,
        string unsubscribeUrl,
        string ctaUrl)
    {
        var firstName = !string.IsNullOrEmpty(customer?.NormalizedName)
            ? customer!.NormalizedName.Split(' ')[0]
            : ExtractFirstName(queueItem.RecipientName);
        var company = customer?.CompanyName ?? "sua agência";
        var leadStatus = customer?.Status.ToString() ?? CustomerStatus.Lead.ToString();

        var dict = BuildTemplateVariables(firstName, queueItem.RecipientEmail, company, leadStatus);

        // Custom variables for pro campaigns
        dict["nome"] = firstName;
        dict["empresa"] = company;
        dict["site"] = customer?.Website ?? string.Empty;
        dict["cidade"] = string.Empty; // Not in standard database schema

        // Detect current tool from customer tags
        var currentTool = "planilhas ou CRM";
        if (customer?.Tags != null)
        {
            var tagsLower = customer.Tags.ToLowerInvariant();
            if (tagsLower.Contains("pipedrive")) currentTool = "Pipedrive";
            else if (tagsLower.Contains("rd station") || tagsLower.Contains("rdstation")) currentTool = "RD Station";
            else if (tagsLower.Contains("notion")) currentTool = "Notion";
            else if (tagsLower.Contains("planilha") || tagsLower.Contains("sheets")) currentTool = "planilhas";
        }
        dict["ferramenta_atual"] = currentTool;

        // Detect main pain from customer tags
        var mainPain = "descentralização de contatos comercial e financeiro";
        if (customer?.Tags != null)
        {
            var tagsLower = customer.Tags.ToLowerInvariant();
            if (tagsLower.Contains("whatsapp")) mainPain = "falta de integração com o WhatsApp";
            else if (tagsLower.Contains("financeiro") || tagsLower.Contains("cobranca")) mainPain = "perda de tempo com cobranças manuais";
        }
        dict["dor_principal"] = mainPain;

        // CTA e unsubscribe vêm do UnsubscribeLinkBuilder (host público real + token HMAC).
        // Os antigos hardcodes apontavam para diaxcrm.com.br — domínio morto: nenhum
        // destinatário conseguia se descadastrar.
        dict["cta_link"] = ctaUrl;
        dict["unsubscribe_url"] = unsubscribeUrl;

        return dict;
    }

    private sealed record ResolvedCampaignTemplateSnapshot(string TemplateBody, string Source);

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Result<string?> BuildAttachmentsJson(List<EmailAttachmentRequestDto>? attachments)
    {
        if (attachments is null || attachments.Count == 0)
        {
            return Result.Success<string?>(null);
        }

        var normalized = new List<EmailAttachmentRequestDto>();
        var totalBytes = 0;

        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.FileName) || string.IsNullOrWhiteSpace(attachment.Base64Content))
            {
                return Result.Failure<string?>(
                    Error.Validation("Attachments", "Anexo inválido. Informe nome do arquivo e conteúdo em base64."));
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(attachment.Base64Content);
            }
            catch
            {
                return Result.Failure<string?>(
                    Error.Validation("Attachments", $"Anexo '{attachment.FileName}' possui base64 inválido."));
            }

            totalBytes += bytes.Length;
            if (totalBytes > MaxAttachmentBytes)
            {
                return Result.Failure<string?>(
                    Error.Validation("Attachments", "Tamanho total dos anexos excede 10MB."));
            }

            normalized.Add(new EmailAttachmentRequestDto
            {
                FileName = attachment.FileName,
                Base64Content = attachment.Base64Content,
                ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/octet-stream"
                    : attachment.ContentType
            });
        }

        var attachmentsJson = JsonSerializer.Serialize(normalized);
        return Result.Success<string?>(attachmentsJson);
    }

    public async Task<Result<EmailAnalyticsSummaryResponse>> GetAnalyticsSummaryAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<EmailAnalyticsSummaryResponse>(
                Error.Unauthorized("Usuário não autenticado."));
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        // Get recent campaigns for current user
        var allCampaigns = await _emailCampaignRepository.FindAsync(
            c => c.UserId == _currentUserService.UserId.Value && c.CreatedAt >= cutoffDate,
            cancellationToken);

        var campaigns = allCampaigns.OrderByDescending(c => c.CreatedAt).ToList();

        // Calculate overall stats
        var overallStats = new OverallStatsDto
        {
            TotalCampaigns = campaigns.Count,
            TotalEmailsSent = campaigns.Sum(c => c.SentCount),
            TotalDelivered = campaigns.Sum(c => c.DeliveredCount),
            TotalOpened = campaigns.Sum(c => c.OpenCount),
            TotalClicks = campaigns.Sum(c => c.ClickCount),
            TotalBounces = campaigns.Sum(c => c.BounceCount),
            TotalUnsubscribes = campaigns.Sum(c => c.UnsubscribeCount),
        };

        // Map campaigns to DTOs
        var campaignStats = campaigns.Select(c => new CampaignStatsDto
        {
            Id = c.Id,
            Name = c.Name,
            Subject = c.Subject,
            CreatedAt = c.CreatedAt,
            TotalRecipients = c.TotalRecipients,
            SentCount = c.SentCount,
            DeliveredCount = c.DeliveredCount,
            OpenCount = c.OpenCount,
            ClickCount = c.ClickCount,
            BounceCount = c.BounceCount,
            UnsubscribeCount = c.UnsubscribeCount,
            FailedCount = c.FailedCount,
        }).ToList();

        // Calculate daily engagement trend
        var engagementTrend = new EngagementTrendDto
        {
            DailyData = campaigns
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new DailyEngagementDto
                {
                    Date = g.Key,
                    Sent = g.Sum(c => c.SentCount),
                    Opened = g.Sum(c => c.OpenCount),
                    Clicked = g.Sum(c => c.ClickCount),
                })
                .OrderBy(d => d.Date)
                .ToList()
        };

        return new EmailAnalyticsSummaryResponse
        {
            OverallStats = overallStats,
            RecentCampaigns = campaignStats,
            EngagementTrend = engagementTrend,
        };
    }

    public async Task<Result<QueueCampaignRecipientsResponse>> QueueWithSmartAssignmentAsync(
        Pro.Dtos.QueueWithAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId is null)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Unauthorized("Usuário não autenticado."));
        }

        if (request.Leads.Count == 0)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Leads", "Informe ao menos um lead para enfileirar."));
        }

        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(request.CampaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(campaignResult.Error);
        }

        var campaign = campaignResult.Value;

        if (_circuitBreaker.IsOpen)
        {
            await LogPilotEventAsync("PilotRealSendBlocked", "Blocked", campaign.Id, request.Leads.Count, false, $"Circuit Breaker aberto: {_circuitBreaker.Reason}", cancellationToken);
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("CircuitBreaker", $"Envios reais bloqueados pelo Circuit Breaker: {_circuitBreaker.Reason}"));
        }

        var readinessResult = ValidateReadiness(campaign);
        await LogPilotEventAsync("PilotReadinessChecked", readinessResult.IsSuccess ? "Success" : "Failed", campaign.Id, request.Leads.Count, false, readinessResult.IsSuccess ? null : readinessResult.Error.Message, cancellationToken);
        if (readinessResult.IsFailure)
        {
            await LogPilotEventAsync("PilotReadinessBlocked", "Blocked", campaign.Id, request.Leads.Count, false, readinessResult.Error.Message, cancellationToken);
            await LogPilotEventAsync("PilotCampaignActivationBlocked", "Blocked", campaign.Id, request.Leads.Count, false, readinessResult.Error.Message, cancellationToken);
            return Result.Failure<QueueCampaignRecipientsResponse>(readinessResult.Error);
        }

        if (campaign.Status is EmailCampaignStatus.Completed or EmailCampaignStatus.Cancelled)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Status", "Campanhas concluídas/canceladas não podem receber novos envios."));
        }

        var templateResult = await ResolveCampaignTemplateSnapshotAsync(campaign, null, null, cancellationToken);
        if (templateResult.IsFailure)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(templateResult.Error);
        }

        var customerIds = request.Leads.Select(l => l.CustomerId).ToList();
        var customers = (await _customerRepository.FindAsync(
            c => customerIds.Contains(c.Id), cancellationToken))
            .ToDictionary(c => c.Id);

        var scheduledAt = NormalizeSchedule(null);
        var queuedItems = new List<EmailQueueItem>();
        var skipped = new List<string>();

        var existingQueued = await _emailQueueRepository.FindAsync(
            q => q.CampaignId == campaign.Id && q.CustomerId.HasValue && customerIds.Contains(q.CustomerId.Value),
            cancellationToken);
        
        var existingQueuedCustomerIds = existingQueued
            .Where(q => q.CustomerId.HasValue)
            .Select(q => q.CustomerId!.Value)
            .ToHashSet();

        var enabledForAssignment = _providerPolicy.EnabledProviders;
        if (enabledForAssignment.Count == 0)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Providers", "Nenhum provider de email habilitado (Email:DisabledProviders)."));
        }

        foreach (var leadDto in request.Leads)
        {
            if (!customers.TryGetValue(leadDto.CustomerId, out var customer))
            {
                skipped.Add($"{leadDto.CustomerId} (não encontrado)");
                continue;
            }

            if (!IsValidEmail(customer.Email))
            {
                skipped.Add($"{customer.Name} (email inválido)");
                continue;
            }

            // Consentimento: mesmo gate do caminho legado (QueueCampaignRecipients).
            // Sem estes checks, quem fez opt-out (inclusive via hard bounce) voltava
            // a ser enfileirado em qualquer campanha nova — violação de LGPD/CAN-SPAM.
            if (customer.EmailOptOut)
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (opt-out ativo)");
                continue;
            }

            if (await _suppressionRepository.IsSuppressedAsync(_currentUserService.UserId!.Value, customer.Email, cancellationToken))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (suprimido)");
                continue;
            }

            if (existingQueuedCustomerIds.Contains(customer.Id))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (já enfileirado ou enviado para esta campanha)");
                continue;
            }

            // Parse estrito: nome de provider desconhecido é erro do caller — o antigo
            // default silencioso para Brevo mascarava typos e desequilibrava limites.
            EmailProvider provider;
            if (string.IsNullOrWhiteSpace(leadDto.AssignedProvider))
            {
                provider = enabledForAssignment[0];
            }
            else if (!_providerPolicy.TryParse(leadDto.AssignedProvider, out provider))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (provider desconhecido: '{leadDto.AssignedProvider}')");
                continue;
            }
            else if (!_providerPolicy.IsEnabled(provider))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (provider desabilitado: '{leadDto.AssignedProvider}')");
                continue;
            }

            queuedItems.Add(new EmailQueueItem(
                _currentUserService.UserId.Value,
                customer.Name,
                customer.Email!,
                campaign.Subject,
                templateResult.Value.TemplateBody,
                scheduledAt,
                customer.Id,
                null,
                campaign.Id,
                provider));
        }

        if (queuedItems.Count == 0)
        {
            return Result.Failure<QueueCampaignRecipientsResponse>(
                Error.Validation("Recipients", "Nenhum destinatário válido para enfileirar."));
        }

        campaign.SetTotalRecipients(queuedItems.Count);
        campaign.StartProcessing();

        await _emailCampaignRepository.UpdateAsync(campaign, cancellationToken);
        await _emailQueueRepository.AddRangeAsync(queuedItems, cancellationToken);

        // Registra envio em cada customer para habilitar dedup cross-day via LastEmailSentAt/EmailSentCount
        var queuedCustomerIds = queuedItems
            .Where(i => i.CustomerId.HasValue)
            .Select(i => i.CustomerId!.Value)
            .ToHashSet();
        foreach (var customer in customers.Values.Where(c => queuedCustomerIds.Contains(c.Id)))
        {
            customer.RegisterEmailSent();
            await _customerRepository.UpdateAsync(customer, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new QueueCampaignRecipientsResponse
        {
            CampaignId = campaign.Id,
            RequestedCount = request.Leads.Count,
            QueuedCount = queuedItems.Count,
            SkippedCount = skipped.Count,
            EffectiveScheduledAt = scheduledAt,
            SkippedRecipients = skipped,
        };
    }

    public static Result ValidateContentReadiness(EmailCampaign campaign)
    {
        if (string.IsNullOrWhiteSpace(campaign.BodyHtml))
        {
            return Result.Failure(Error.Validation("ReadinessGate", "O conteúdo da campanha está em branco."));
        }

        // 1. Unsubscribe check
        if (!campaign.BodyHtml.Contains("{{unsubscribe_url}}", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Validation("ReadinessGate", "A campanha não possui a variável obrigatória de cancelamento de inscrição {{unsubscribe_url}} no rodapé."));
        }

        // 2. CTA and UTM check
        if (campaign.BodyHtml.Contains("href=", StringComparison.OrdinalIgnoreCase) && 
            !campaign.BodyHtml.Contains("utm_source=", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Validation("ReadinessGate", "Os links da campanha precisam conter parâmetros de tracking obrigatórios (UTMs)."));
        }

        return Result.Success();
    }

    public static Result ValidateReadiness(EmailCampaign campaign)
    {
        var contentResult = ValidateContentReadiness(campaign);
        if (contentResult.IsFailure)
        {
            return contentResult;
        }

        // 3. Draft prevention for outbound campaigns
        // Campanhas em estado Rascunho não podem ser disparadas para listas reais.
        if (campaign.Status == EmailCampaignStatus.Draft)
        {
            return Result.Failure(Error.Validation("ReadinessGate", "A campanha está em estado Rascunho (Draft). Ative ou agende a campanha para permitir disparos de outbound."));
        }

        // Environment check
        var allowedEnvs = new[] { "Development", "Production", "Test" };
        var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        if (!allowedEnvs.Contains(currentEnv, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Validation("ReadinessGate", $"Envios não são permitidos no ambiente atual: {currentEnv}"));
        }

        return Result.Success();
    }

    private async Task LogPilotEventAsync(
        string action,
        string result,
        Guid campaignId,
        int leadCount,
        bool dryRun,
        string? blockingReasons,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        string userEmail = "sistema";
        if (userId.HasValue)
        {
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

    public async Task<Result<PilotStatusResponse>> GetPilotStatusAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaignResult = await GetCampaignOwnedByCurrentUserAsync(campaignId, cancellationToken);
        if (campaignResult.IsFailure)
        {
            return Result.Failure<PilotStatusResponse>(campaignResult.Error);
        }

        var campaign = campaignResult.Value;
        var readiness = ValidateReadiness(campaign);

        var auditEntries = await _auditLogRepository.GetByResourceAsync("PilotCampaign", campaignId.ToString(), cancellationToken);
        var recentEvents = auditEntries
            .OrderByDescending(a => a.TimestampUtc)
            .Take(20)
            .Select(a => {
                try 
                {
                    var details = JsonSerializer.Deserialize<JsonElement>(a.NewValues ?? "{}");
                    return new PilotEventDto
                    {
                        Action = details.TryGetProperty("Action", out var act) ? act.GetString() ?? a.Description : a.Description,
                        Result = details.TryGetProperty("Result", out var res) ? res.GetString() ?? "" : "",
                        BlockingReasons = details.TryGetProperty("BlockingReasons", out var blk) ? blk.GetString() : null,
                        LeadCount = details.TryGetProperty("LeadCount", out var lc) ? lc.GetInt32() : 0,
                        DryRun = details.TryGetProperty("DryRun", out var dr) ? dr.GetBoolean() : false,
                        TimestampUtc = a.TimestampUtc,
                        UserEmail = details.TryGetProperty("UserEmail", out var ue) ? ue.GetString() ?? "sistema" : "sistema"
                    };
                }
                catch
                {
                    return new PilotEventDto
                    {
                        Action = a.Description,
                        Result = a.Status.ToString(),
                        TimestampUtc = a.TimestampUtc,
                        UserEmail = "sistema"
                    };
                }
            })
            .ToList();

        var response = new PilotStatusResponse
        {
            IsCircuitBreakerOpen = _circuitBreaker.IsOpen,
            CircuitBreakerReason = _circuitBreaker.Reason,
            CurrentErrorRate = _circuitBreaker.CurrentErrorRate,
            WebhookFailureCount = _circuitBreaker.WebhookFailureCount,
            CampaignReadinessPassed = readiness.IsSuccess,
            CampaignReadinessError = readiness.IsFailure ? readiness.Error.Message : null,
            RecentEvents = recentEvents
        };

        return Result.Success(response);
    }

    /// <summary>
    /// Reset manual (admin) do Circuit Breaker do piloto. Fecha o circuito e limpa a
    /// janela de falhas, dispensando o restart da aplicação. Não há reabertura automática.
    /// </summary>
    public async Task<Result<PilotResetResponse>> ResetCircuitBreakerAsync(CancellationToken cancellationToken = default)
    {
        var wasOpen = _circuitBreaker.IsOpen;
        var previousReason = _circuitBreaker.Reason;

        _circuitBreaker.Reset();

        // Auditoria: reabilitar envios reais após um trip é justamente o evento que
        // mais importa rastrear (quem, quando, e qual era o motivo do bloqueio).
        await LogPilotEventAsync(
            "PilotCircuitBreakerReset",
            "Reset",
            Guid.Empty,
            0,
            false,
            wasOpen ? $"Reset manual; motivo anterior: {previousReason}" : "Reset manual (circuito já estava fechado)",
            cancellationToken);

        var response = new PilotResetResponse
        {
            WasOpen = wasOpen,
            PreviousReason = previousReason,
            IsOpenNow = _circuitBreaker.IsOpen
        };

        return Result.Success(response);
    }
}
