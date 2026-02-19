using System.Net.Mail;
using System.Text.Json;
using Diax.Application.Common;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Domain.Snippets;
using Diax.Shared.Results;

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

    public EmailMarketingService(
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICustomerRepository customerRepository,
        ISnippetRepository snippetRepository,
        IEmailTemplateEngine emailTemplateEngine,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _emailQueueRepository = emailQueueRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _customerRepository = customerRepository;
        _snippetRepository = snippetRepository;
        _emailTemplateEngine = emailTemplateEngine;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
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

        foreach (var customer in recipients)
        {
            if (!IsValidEmail(customer.Email))
            {
                skipped.Add($"{customer.Name} <{customer.Email}> (email inválido)");
                continue;
            }

            queuedItems.Add(new EmailQueueItem(
                _currentUserService.UserId!.Value,
                customer.Name,
                customer.Email,
                campaign.Subject,
                templateResult.Value.TemplateBody,
                scheduledAt,
                customer.Id,
                attachmentsJsonResult.Value,
                campaign.Id));
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

    public static Dictionary<string, string?> BuildRecipientTemplateVariables(Customer? customer, EmailQueueItem queueItem)
    {
        var firstName = ExtractFirstName(queueItem.RecipientName);
        var company = customer?.CompanyName;
        var leadStatus = customer?.Status.ToString() ?? CustomerStatus.Lead.ToString();

        return BuildTemplateVariables(firstName, queueItem.RecipientEmail, company, leadStatus);
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

    private static Result<string?> BuildAttachmentsJson(List<EmailAttachmentRequestDto>? attachments)
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
}
