using Diax.Application.Common;
using Diax.Application.EmailMarketing;
using Diax.Application.Outreach.Dtos;
using Diax.Application.WhatsApp;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Domain.Outreach;
using Diax.Shared.Results;

namespace Diax.Application.Outreach;

/// <summary>
/// Serviço de aplicação responsável pelo módulo de outreach automatizado.
/// Gerencia configuração, segmentação de leads e envio de campanhas.
/// </summary>
public class OutreachService : IApplicationService
{
    private readonly IOutreachConfigRepository _configRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSender _whatsAppSender;

    /// <summary>
    /// Cidades brasileiras prioritárias para pontuação de leads.
    /// </summary>
    private static readonly string[] PriorityCities =
    [
        "são paulo", "rio de janeiro", "belo horizonte", "curitiba",
        "porto alegre", "brasília", "salvador", "fortaleza",
        "recife", "campinas", "florianópolis", "goiânia",
        "manaus", "belém", "vitória", "santos",
        "joinville", "ribeirão preto", "uberlândia", "maringá"
    ];

    public OutreachService(
        IOutreachConfigRepository configRepository,
        ICustomerRepository customerRepository,
        IEmailQueueRepository emailQueueRepository,
        IEmailCampaignRepository emailCampaignRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IWhatsAppSender whatsAppSender)
    {
        _configRepository = configRepository;
        _customerRepository = customerRepository;
        _emailQueueRepository = emailQueueRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _whatsAppSender = whatsAppSender;
    }

    // ===== CONFIGURAÇÃO =====

    /// <summary>
    /// Obtém ou cria a configuração de outreach do usuário atual.
    /// </summary>
    public async Task<Result<OutreachConfigResponse>> GetOrCreateConfigAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<OutreachConfigResponse>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        if (config is null)
        {
            config = new OutreachConfig(userId.Value);
            await _configRepository.AddAsync(config, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(OutreachConfigResponse.FromEntity(config));
    }

    /// <summary>
    /// Atualiza a configuração de outreach do usuário atual.
    /// </summary>
    public async Task<Result<OutreachConfigResponse>> UpdateConfigAsync(
        UpdateOutreachConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<OutreachConfigResponse>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        if (config is null)
        {
            config = new OutreachConfig(userId.Value);
            await _configRepository.AddAsync(config, cancellationToken);
        }

        config.UpdateApifyConfig(request.ApifyDatasetUrl, request.ApifyApiToken);
        config.UpdateModuleFlags(request.ImportEnabled, request.SegmentationEnabled, request.SendEnabled);
        config.UpdateSendLimits(request.DailyEmailLimit, request.EmailCooldownDays);

        if (request.HotTemplateSubject is not null || request.HotTemplateBody is not null)
            config.UpdateHotTemplate(request.HotTemplateSubject ?? string.Empty, request.HotTemplateBody ?? string.Empty);

        if (request.WarmTemplateSubject is not null || request.WarmTemplateBody is not null)
            config.UpdateWarmTemplate(request.WarmTemplateSubject ?? string.Empty, request.WarmTemplateBody ?? string.Empty);

        if (request.ColdTemplateSubject is not null || request.ColdTemplateBody is not null)
            config.UpdateColdTemplate(request.ColdTemplateSubject ?? string.Empty, request.ColdTemplateBody ?? string.Empty);

        // WhatsApp config
        config.UpdateWhatsAppFlags(request.WhatsAppSendEnabled);
        config.UpdateWhatsAppLimits(request.DailyWhatsAppLimit, request.WhatsAppCooldownDays);
        config.UpdateWhatsAppTemplates(
            request.WhatsAppHotTemplate,
            request.WhatsAppWarmTemplate,
            request.WhatsAppColdTemplate,
            request.WhatsAppFollowUpTemplate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OutreachConfigResponse.FromEntity(config));
    }

    // ===== SEGMENTAÇÃO =====

    /// <summary>
    /// Segmenta leads não classificados baseado em critérios de pontuação.
    /// Critérios: Website (+2), Phone (+1), Tags não vazio (+1), Cidade prioritária (+2).
    /// Segmento: Hot >= 5, Warm >= 3, Cold &lt; 3.
    /// </summary>
    public async Task<Result<SegmentationResultResponse>> SegmentLeadsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<SegmentationResultResponse>(Error.Unauthorized());

        // Buscar leads sem segmentação
        var unsegmentedLeads = await _customerRepository.FindAsync(
            c => c.Status == CustomerStatus.Lead
                 && c.Segment == null
                 && c.LeadScore == null,
            cancellationToken);

        var leads = unsegmentedLeads.ToList();
        var result = new SegmentationResultResponse();

        foreach (var lead in leads)
        {
            var score = CalculateLeadScore(lead);
            var segment = score >= 5 ? LeadSegment.Hot
                        : score >= 3 ? LeadSegment.Warm
                        : LeadSegment.Cold;

            lead.UpdateSegmentation(score, segment);

            switch (segment)
            {
                case LeadSegment.Hot:
                    result.HotCount++;
                    break;
                case LeadSegment.Warm:
                    result.WarmCount++;
                    break;
                case LeadSegment.Cold:
                    result.ColdCount++;
                    break;
            }
        }

        result.TotalProcessed = leads.Count;

        if (leads.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result);
    }

    /// <summary>
    /// Calcula o score do lead baseado nos critérios definidos.
    /// </summary>
    private static int CalculateLeadScore(Customer lead)
    {
        var score = 0;

        // Website presente: +2
        if (!string.IsNullOrWhiteSpace(lead.Website))
            score += 2;

        // Telefone presente: +1
        if (!string.IsNullOrWhiteSpace(lead.Phone))
            score += 1;

        // Tags não vazio: +1
        if (!string.IsNullOrWhiteSpace(lead.Tags))
        {
            score += 1;

            // Cidade prioritária nas tags: +2
            var tagsLower = lead.Tags.ToLowerInvariant();
            if (PriorityCities.Any(city => tagsLower.Contains(city)))
                score += 2;
        }

        return score;
    }

    // ===== DASHBOARD =====

    /// <summary>
    /// Obtém métricas do dashboard de outreach.
    /// </summary>
    public async Task<Result<OutreachDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<OutreachDashboardResponse>(Error.Unauthorized());

        // Buscar configuração
        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        // Buscar todos os leads (Status == Lead)
        var allLeads = (await _customerRepository.GetByStatusAsync(CustomerStatus.Lead, cancellationToken)).ToList();

        var hotLeads = allLeads.Count(c => c.Segment == LeadSegment.Hot);
        var warmLeads = allLeads.Count(c => c.Segment == LeadSegment.Warm);
        var coldLeads = allLeads.Count(c => c.Segment == LeadSegment.Cold);
        var unsegmented = allLeads.Count(c => c.Segment == null);

        // Contagem de emails enviados
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var emailsSentToday = await _emailQueueRepository.CountSentSinceAsync(todayStart, cancellationToken);
        var emailsSentThisWeek = await _emailQueueRepository.CountSentSinceAsync(weekStart, cancellationToken);
        var emailsSentThisMonth = await _emailQueueRepository.CountSentSinceAsync(monthStart, cancellationToken);

        // Contagem de pendentes e falhados na fila
        var queueItems = await _emailQueueRepository.FindAsync(
            q => q.UserId == userId.Value,
            cancellationToken);

        var queueList = queueItems.ToList();
        var pendingCount = queueList.Count(q => q.Status == EmailQueueStatus.Queued);
        var failedCount = queueList.Count(q => q.Status == EmailQueueStatus.Failed);

        // WhatsApp stats
        var whatsAppCooldownDays = config?.WhatsAppCooldownDays ?? 3;
        var whatsAppCooldownDate = DateTime.UtcNow.AddDays(-whatsAppCooldownDays);

        var whatsAppSentToday = allLeads.Count(c => c.LastWhatsAppSentAt >= todayStart);
        var whatsAppSentThisWeek = allLeads.Count(c => c.LastWhatsAppSentAt >= weekStart);
        var whatsAppReadyCount = allLeads.Count(c =>
            c.Segment != null
            && (c.WhatsApp != null || c.Phone != null)
            && !c.WhatsAppOptOut
            && (c.LastWhatsAppSentAt == null || c.LastWhatsAppSentAt < whatsAppCooldownDate));

        var whatsAppConnectionStatus = "unknown";
        try
        {
            var waStatus = await _whatsAppSender.GetConnectionStatusAsync(cancellationToken);
            whatsAppConnectionStatus = waStatus.IsConnected ? "connected" : waStatus.State;
        }
        catch
        {
            whatsAppConnectionStatus = "error";
        }

        return Result.Success(new OutreachDashboardResponse
        {
            TotalLeads = allLeads.Count,
            HotLeads = hotLeads,
            WarmLeads = warmLeads,
            ColdLeads = coldLeads,
            UnsegmentedLeads = unsegmented,
            EmailsSentToday = emailsSentToday,
            EmailsSentThisWeek = emailsSentThisWeek,
            EmailsSentThisMonth = emailsSentThisMonth,
            PendingInQueue = pendingCount,
            FailedInQueue = failedCount,
            ImportEnabled = config?.ImportEnabled ?? false,
            SegmentationEnabled = config?.SegmentationEnabled ?? false,
            SendEnabled = config?.SendEnabled ?? false,
            WhatsAppSendEnabled = config?.WhatsAppSendEnabled ?? false,
            WhatsAppSentToday = whatsAppSentToday,
            WhatsAppSentThisWeek = whatsAppSentThisWeek,
            WhatsAppReadyCount = whatsAppReadyCount,
            WhatsAppConnectionStatus = whatsAppConnectionStatus
        });
    }

    // ===== LEADS PRONTOS =====

    /// <summary>
    /// Obtém leads segmentados prontos para envio de outreach.
    /// Filtra por: email válido, não opt-out, respeita cooldown de envio.
    /// Ordena por segmento (Hot primeiro) e depois por data de criação.
    /// </summary>
    public async Task<Result<List<ReadyLeadResponse>>> GetReadyLeadsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<List<ReadyLeadResponse>>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        var cooldownDays = config?.EmailCooldownDays ?? 7;
        var cooldownDate = DateTime.UtcNow.AddDays(-cooldownDays);

        // Buscar leads segmentados com email válido
        var candidates = await _customerRepository.FindAsync(
            c => c.Status == CustomerStatus.Lead
                 && c.Segment != null
                 && c.Email != null
                 && c.Email != string.Empty
                 && !c.Email.Contains("@placeholder.local")
                 && !c.EmailOptOut
                 && (c.LastEmailSentAt == null || c.LastEmailSentAt < cooldownDate),
            cancellationToken);

        var readyLeads = candidates
            .OrderByDescending(c => (int)(c.Segment ?? LeadSegment.Cold))
            .ThenBy(c => c.CreatedAt)
            .Take(limit)
            .Select(ReadyLeadResponse.FromEntity)
            .ToList();

        return Result.Success(readyLeads);
    }

    // ===== CRIAÇÃO E ENVIO DE CAMPANHA =====

    /// <summary>
    /// Cria campanhas de email agrupadas por segmento e enfileira os emails para envio.
    /// Respeita o limite diário de emails e o cooldown entre envios.
    /// </summary>
    public async Task<Result<OutreachSendResultResponse>> CreateAndQueueCampaignAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<OutreachSendResultResponse>(Error.Unauthorized());

        // Buscar configuração
        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        if (config is null)
            return Result.Failure<OutreachSendResultResponse>(
                Error.Validation("Outreach.NoConfig", "Configure o outreach antes de enviar campanhas."));

        if (!config.SendEnabled)
            return Result.Failure<OutreachSendResultResponse>(
                Error.Validation("Outreach.SendDisabled", "O envio de emails está desabilitado nas configurações."));

        // Verificar limite diário
        var todayStart = DateTime.UtcNow.Date;
        var sentToday = await _emailQueueRepository.CountSentSinceAsync(todayStart, cancellationToken);
        var remainingBudget = config.DailyEmailLimit - sentToday;

        if (remainingBudget <= 0)
            return Result.Failure<OutreachSendResultResponse>(
                Error.Validation("Outreach.DailyLimitReached",
                    $"Limite diário de {config.DailyEmailLimit} emails já foi atingido."));

        // Buscar leads prontos respeitando o orçamento restante
        var readyLeadsResult = await GetReadyLeadsAsync(remainingBudget, cancellationToken);
        if (readyLeadsResult.IsFailure)
            return Result.Failure<OutreachSendResultResponse>(readyLeadsResult.Error);

        var cooldownDays = config.EmailCooldownDays;
        var cooldownDate = DateTime.UtcNow.AddDays(-cooldownDays);

        // Buscar entidades reais dos leads para poder chamar RegisterEmailSent
        var readyLeadIds = readyLeadsResult.Value.Select(r => r.Id).ToHashSet();
        var leadEntities = (await _customerRepository.FindAsync(
            c => readyLeadIds.Contains(c.Id),
            cancellationToken)).ToList();

        var result = new OutreachSendResultResponse();
        var skippedReasons = new List<string>();
        var totalQueued = 0;
        var lastCampaignId = (Guid?)null;

        // Agrupar por segmento
        var segmentGroups = leadEntities
            .Where(c => c.Segment.HasValue)
            .GroupBy(c => c.Segment!.Value)
            .OrderByDescending(g => (int)g.Key);

        foreach (var group in segmentGroups)
        {
            var segment = group.Key;
            var leads = group.ToList();

            // Obter template do segmento
            var (subject, body) = GetTemplateForSegment(config, segment);

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                skippedReasons.Add($"Template não configurado para segmento {segment}. {leads.Count} leads ignorados.");
                result.SkippedCount += leads.Count;
                continue;
            }

            // Criar campanha
            var campaignName = $"Outreach {segment} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
            var campaign = new EmailCampaign(userId.Value, campaignName, subject, body);
            campaign.SetTotalRecipients(leads.Count);
            campaign.Schedule(DateTime.UtcNow);
            campaign.StartProcessing();

            await _emailCampaignRepository.AddAsync(campaign, cancellationToken);
            lastCampaignId = campaign.Id;

            // Criar itens de fila
            var queueItems = new List<EmailQueueItem>();

            foreach (var lead in leads)
            {
                if (string.IsNullOrWhiteSpace(lead.Email) || lead.Email.Contains("@placeholder.local"))
                {
                    skippedReasons.Add($"Lead '{lead.Name}' sem email válido.");
                    result.SkippedCount++;
                    continue;
                }

                // Personalizar template com dados do lead
                var personalizedSubject = PersonalizeTemplate(subject, lead);
                var personalizedBody = PersonalizeTemplate(body, lead);

                var queueItem = new EmailQueueItem(
                    userId: userId.Value,
                    recipientName: lead.Name,
                    recipientEmail: lead.Email,
                    subject: personalizedSubject,
                    htmlBody: personalizedBody,
                    scheduledAt: DateTime.UtcNow,
                    customerId: lead.Id,
                    campaignId: campaign.Id);

                queueItems.Add(queueItem);
                lead.RegisterEmailSent();
                totalQueued++;
            }

            if (queueItems.Count > 0)
                await _emailQueueRepository.AddRangeAsync(queueItems, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        result.CampaignId = lastCampaignId;
        result.QueuedCount = totalQueued;
        result.SkippedReasons = skippedReasons;

        return Result.Success(result);
    }

    // ===== WHATSAPP =====

    /// <summary>
    /// Verifica o status de conexão da instância WhatsApp.
    /// </summary>
    public async Task<Result<WhatsAppConnectionStatus>> GetWhatsAppStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = await _whatsAppSender.GetConnectionStatusAsync(cancellationToken);
        return Result.Success(status);
    }

    /// <summary>
    /// Obtém leads prontos para envio de WhatsApp.
    /// Filtra por: WhatsApp ou Phone válido, não opt-out, respeita cooldown.
    /// </summary>
    public async Task<Result<List<WhatsAppReadyLeadResponse>>> GetWhatsAppReadyLeadsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<List<WhatsAppReadyLeadResponse>>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        var cooldownDays = config?.WhatsAppCooldownDays ?? 3;
        var cooldownDate = DateTime.UtcNow.AddDays(-cooldownDays);

        var candidates = await _customerRepository.FindAsync(
            c => c.Status == CustomerStatus.Lead
                 && c.Segment != null
                 && (c.WhatsApp != null || c.Phone != null)
                 && !c.WhatsAppOptOut
                 && (c.LastWhatsAppSentAt == null || c.LastWhatsAppSentAt < cooldownDate),
            cancellationToken);

        var readyLeads = candidates
            .OrderByDescending(c => (int)(c.Segment ?? LeadSegment.Cold))
            .ThenBy(c => c.CreatedAt)
            .Take(limit)
            .Select(WhatsAppReadyLeadResponse.FromEntity)
            .ToList();

        return Result.Success(readyLeads);
    }

    /// <summary>
    /// Envia WhatsApp individual para um cliente/lead.
    /// </summary>
    public async Task<Result<WhatsAppSendResult>> SendWhatsAppToCustomerAsync(
        WhatsAppSendRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<WhatsAppSendResult>(Error.Unauthorized());

        string? whatsappNumber = request.PhoneNumber;
        Customer? customer = null;

        if (request.CustomerId.HasValue)
        {
            var customers = await _customerRepository.FindAsync(
                c => c.Id == request.CustomerId.Value,
                cancellationToken);
            customer = customers.FirstOrDefault();

            if (customer is null)
                return Result.Failure<WhatsAppSendResult>(
                    Error.NotFound("Customer", request.CustomerId.Value));

            whatsappNumber = customer.WhatsApp ?? customer.Phone;
        }

        if (string.IsNullOrWhiteSpace(whatsappNumber))
            return Result.Failure<WhatsAppSendResult>(
                Error.Validation("WhatsApp.NoNumber", "Nenhum número de WhatsApp disponível."));

        if (string.IsNullOrWhiteSpace(request.Message))
            return Result.Failure<WhatsAppSendResult>(
                Error.Validation("WhatsApp.NoMessage", "A mensagem não pode estar vazia."));

        // Personalizar mensagem se tiver dados do customer
        var message = customer != null ? PersonalizeTemplate(request.Message, customer) : request.Message;

        var result = await _whatsAppSender.SendTextAsync(whatsappNumber, message, cancellationToken);

        if (result.Success && customer != null)
        {
            customer.RegisterWhatsAppSent();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(result);
    }

    /// <summary>
    /// Envia campanha WhatsApp em massa para leads segmentados.
    /// Respeita limites diários e cooldown entre envios.
    /// </summary>
    public async Task<Result<WhatsAppSendResultResponse>> SendWhatsAppCampaignAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<WhatsAppSendResultResponse>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        if (config is null)
            return Result.Failure<WhatsAppSendResultResponse>(
                Error.Validation("Outreach.NoConfig", "Configure o outreach antes de enviar campanhas WhatsApp."));

        if (!config.WhatsAppSendEnabled)
            return Result.Failure<WhatsAppSendResultResponse>(
                Error.Validation("WhatsApp.SendDisabled", "O envio de WhatsApp está desabilitado nas configurações."));

        // Buscar leads prontos
        var readyResult = await GetWhatsAppReadyLeadsAsync(config.DailyWhatsAppLimit, cancellationToken);
        if (readyResult.IsFailure)
            return Result.Failure<WhatsAppSendResultResponse>(readyResult.Error);

        var readyLeadDtos = readyResult.Value;
        if (readyLeadDtos.Count == 0)
            return Result.Success(new WhatsAppSendResultResponse
            {
                SkippedReasons = new List<string> { "Nenhum lead pronto para envio de WhatsApp." }
            });

        // Buscar entidades reais
        var readyIds = readyLeadDtos.Select(r => r.Id).ToHashSet();
        var leadEntities = (await _customerRepository.FindAsync(
            c => readyIds.Contains(c.Id),
            cancellationToken)).ToList();

        var result = new WhatsAppSendResultResponse();

        // Agrupar por segmento e enviar
        var segmentGroups = leadEntities
            .Where(c => c.Segment.HasValue)
            .GroupBy(c => c.Segment!.Value)
            .OrderByDescending(g => (int)g.Key);

        foreach (var group in segmentGroups)
        {
            var segment = group.Key;
            var template = GetWhatsAppTemplateForSegment(config, segment);

            if (string.IsNullOrWhiteSpace(template))
            {
                result.SkippedReasons.Add($"Template WhatsApp não configurado para segmento {segment}. {group.Count()} leads ignorados.");
                result.SkippedCount += group.Count();
                continue;
            }

            foreach (var lead in group)
            {
                var number = lead.WhatsApp ?? lead.Phone;
                if (string.IsNullOrWhiteSpace(number))
                {
                    result.SkippedReasons.Add($"Lead '{lead.Name}' sem número de WhatsApp.");
                    result.SkippedCount++;
                    continue;
                }

                var personalizedMessage = PersonalizeTemplate(template, lead);
                var sendResult = await _whatsAppSender.SendTextAsync(number, personalizedMessage, cancellationToken);

                if (sendResult.Success)
                {
                    lead.RegisterWhatsAppSent();
                    result.SentCount++;
                }
                else
                {
                    result.FailedReasons.Add($"Falha para '{lead.Name}': {sendResult.Error}");
                    result.FailedCount++;
                }

                // Delay entre envios para evitar bloqueio (2 segundos)
                await Task.Delay(2000, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result);
    }

    /// <summary>
    /// Envia WhatsApp de follow-up para leads que receberam email mas não abriram.
    /// </summary>
    public async Task<Result<WhatsAppSendResultResponse>> SendWhatsAppFollowUpAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<WhatsAppSendResultResponse>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        if (config is null || !config.WhatsAppSendEnabled)
            return Result.Failure<WhatsAppSendResultResponse>(
                Error.Validation("WhatsApp.SendDisabled", "O envio de WhatsApp está desabilitado."));

        if (string.IsNullOrWhiteSpace(config.WhatsAppFollowUpTemplate))
            return Result.Failure<WhatsAppSendResultResponse>(
                Error.Validation("WhatsApp.NoFollowUpTemplate", "Template de follow-up WhatsApp não configurado."));

        // Buscar emails enviados que não foram abertos (status = Sent, não Opened)
        var sentEmails = await _emailQueueRepository.FindAsync(
            q => q.UserId == userId.Value
                 && q.Status == EmailQueueStatus.Sent
                 && q.CustomerId.HasValue,
            cancellationToken);

        var customerIds = sentEmails
            .Select(q => q.CustomerId!.Value)
            .Distinct()
            .ToHashSet();

        if (customerIds.Count == 0)
            return Result.Success(new WhatsAppSendResultResponse
            {
                SkippedReasons = new List<string> { "Nenhum lead com email enviado sem abertura encontrado." }
            });

        // Buscar leads que não deram opt-out de WhatsApp e têm número
        var cooldownDate = DateTime.UtcNow.AddDays(-(config.WhatsAppCooldownDays));
        var candidates = await _customerRepository.FindAsync(
            c => customerIds.Contains(c.Id)
                 && (c.WhatsApp != null || c.Phone != null)
                 && !c.WhatsAppOptOut
                 && (c.LastWhatsAppSentAt == null || c.LastWhatsAppSentAt < cooldownDate),
            cancellationToken);

        var leads = candidates.Take(config.DailyWhatsAppLimit).ToList();
        var result = new WhatsAppSendResultResponse();

        foreach (var lead in leads)
        {
            var number = lead.WhatsApp ?? lead.Phone;
            if (string.IsNullOrWhiteSpace(number))
            {
                result.SkippedCount++;
                continue;
            }

            var message = PersonalizeTemplate(config.WhatsAppFollowUpTemplate, lead);
            var sendResult = await _whatsAppSender.SendTextAsync(number, message, cancellationToken);

            if (sendResult.Success)
            {
                lead.RegisterWhatsAppSent();
                result.SentCount++;
            }
            else
            {
                result.FailedReasons.Add($"Falha para '{lead.Name}': {sendResult.Error}");
                result.FailedCount++;
            }

            await Task.Delay(2000, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(result);
    }

    // ===== EMAIL MARKETING =====

    /// <summary>
    /// Retorna a contagem de contatos com email válido (não nulo, não placeholder, não opt-out).
    /// </summary>
    public async Task<Result<ValidEmailCountResponse>> GetValidEmailCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<ValidEmailCountResponse>(Error.Unauthorized());

        var validEmailContacts = await _customerRepository.FindAsync(
            c => c.Email != null
                 && c.Email != string.Empty
                 && !c.Email.Contains("@placeholder.local")
                 && !c.EmailOptOut,
            cancellationToken);

        return Result.Success(new ValidEmailCountResponse
        {
            Count = validEmailContacts.Count()
        });
    }

    /// <summary>
    /// Envia email marketing personalizado com anexos para TODOS os contatos com email válido.
    /// Diferente do outreach por templates, este aceita HTML e anexos customizados.
    /// </summary>
    public async Task<Result<SendEmailMarketingResponse>> SendEmailMarketingAsync(
        SendEmailMarketingRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<SendEmailMarketingResponse>(Error.Unauthorized());

        if (string.IsNullOrWhiteSpace(request.Subject))
            return Result.Failure<SendEmailMarketingResponse>(
                Error.Validation("Subject", "O assunto é obrigatório."));

        if (string.IsNullOrWhiteSpace(request.BodyHtml))
            return Result.Failure<SendEmailMarketingResponse>(
                Error.Validation("BodyHtml", "O corpo do email é obrigatório."));

        // Validar anexos reutilizando lógica existente
        var attachmentsJsonResult = EmailMarketingService.BuildAttachmentsJson(request.Attachments);
        if (attachmentsJsonResult.IsFailure)
            return Result.Failure<SendEmailMarketingResponse>(attachmentsJsonResult.Error);

        // Buscar TODOS os contatos com email válido (qualquer status)
        var validContacts = (await _customerRepository.FindAsync(
            c => c.Email != null
                 && c.Email != string.Empty
                 && !c.Email.Contains("@placeholder.local")
                 && !c.EmailOptOut,
            cancellationToken)).ToList();

        if (validContacts.Count == 0)
            return Result.Failure<SendEmailMarketingResponse>(
                Error.Validation("Recipients", "Nenhum contato com email válido encontrado."));

        // Criar campanha
        var campaignName = $"Email Marketing - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        var campaign = new EmailCampaign(userId.Value, campaignName, request.Subject, request.BodyHtml);
        campaign.SetTotalRecipients(validContacts.Count);
        campaign.Schedule(DateTime.UtcNow);
        campaign.StartProcessing();
        await _emailCampaignRepository.AddAsync(campaign, cancellationToken);

        // Criar itens de fila
        var queueItems = new List<EmailQueueItem>();
        var skipped = new List<string>();

        foreach (var contact in validContacts)
        {
            if (!IsValidEmailAddress(contact.Email))
            {
                skipped.Add($"{contact.Name} <{contact.Email}> (email inválido)");
                continue;
            }

            var personalizedSubject = PersonalizeTemplate(request.Subject, contact);
            var personalizedBody = PersonalizeTemplate(request.BodyHtml, contact);

            var queueItem = new EmailQueueItem(
                userId: userId.Value,
                recipientName: contact.Name,
                recipientEmail: contact.Email!,
                subject: personalizedSubject,
                htmlBody: personalizedBody,
                scheduledAt: DateTime.UtcNow,
                customerId: contact.Id,
                attachmentsJson: attachmentsJsonResult.Value,
                campaignId: campaign.Id);

            queueItems.Add(queueItem);
        }

        if (queueItems.Count > 0)
            await _emailQueueRepository.AddRangeAsync(queueItems, cancellationToken);

        campaign.SetTotalRecipients(queueItems.Count);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SendEmailMarketingResponse
        {
            CampaignId = campaign.Id,
            TotalValidContacts = validContacts.Count,
            QueuedCount = queueItems.Count,
            SkippedCount = skipped.Count,
            SkippedReasons = skipped
        });
    }

    /// <summary>
    /// Valida se o formato do email é válido.
    /// </summary>
    private static bool IsValidEmailAddress(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ===== MÉTODOS AUXILIARES =====

    /// <summary>
    /// Obtém o template de email (subject e body) para o segmento informado.
    /// </summary>
    private static (string? Subject, string? Body) GetTemplateForSegment(OutreachConfig config, LeadSegment segment)
    {
        return segment switch
        {
            LeadSegment.Hot => (config.HotTemplateSubject, config.HotTemplateBody),
            LeadSegment.Warm => (config.WarmTemplateSubject, config.WarmTemplateBody),
            LeadSegment.Cold => (config.ColdTemplateSubject, config.ColdTemplateBody),
            _ => (null, null)
        };
    }

    /// <summary>
    /// Obtém o template de WhatsApp para o segmento informado.
    /// </summary>
    private static string? GetWhatsAppTemplateForSegment(OutreachConfig config, LeadSegment segment)
    {
        return segment switch
        {
            LeadSegment.Hot => config.WhatsAppHotTemplate,
            LeadSegment.Warm => config.WhatsAppWarmTemplate,
            LeadSegment.Cold => config.WhatsAppColdTemplate,
            _ => null
        };
    }

    /// <summary>
    /// Personaliza o template substituindo placeholders com dados do lead.
    /// Placeholders: {{nome}}, {{empresa}}, {{email}}, {{website}}
    /// </summary>
    private static string PersonalizeTemplate(string template, Customer lead)
    {
        return template
            .Replace("{{nome}}", lead.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{{empresa}}", lead.CompanyName ?? lead.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{{email}}", lead.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{website}}", lead.Website ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
