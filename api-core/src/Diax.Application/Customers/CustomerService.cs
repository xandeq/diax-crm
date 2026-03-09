using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Application.Customers.Services;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Shared.Results;

namespace Diax.Application.Customers;

/// <summary>
/// Serviço de aplicação para operações com Customers/Leads.
/// </summary>
public class CustomerService : IApplicationService
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly ILeadSanitizationService _sanitizationService;

    public CustomerService(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        IEmailQueueRepository emailQueueRepository,
        ILeadSanitizationService sanitizationService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _emailQueueRepository = emailQueueRepository;
        _sanitizationService = sanitizationService;
    }

    /// <summary>
    /// Obtém um customer por ID.
    /// </summary>
    public async Task<Result<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer", id));

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Lista customers com paginação e filtros.
    /// </summary>
    public async Task<PagedResponse<CustomerResponse>> GetPagedAsync(
        CustomerListRequest request,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.Status,
            request.Source,
            request.SortBy,
            request.SortDescending,
            request.HasEmail,
            request.HasWhatsApp,
            request.PersonType,
            request.Segment,
            request.OnlyLeads,
            request.OnlyCustomers,
            request.NeverEmailed,
            request.CreatedAfter,
            cancellationToken);

        var responses = items.Select(CustomerResponse.FromEntity);

        return PagedResponse<CustomerResponse>.Create(responses, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Cria um novo customer/lead.
    /// </summary>
    public async Task<Result<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Executa o Pipeline de Sanitização e Classificação
        var rawData = new RawLeadData(
            request.Name,
            request.Email,
            request.Phone,
            request.WhatsApp,
            request.CompanyName,
            request.Notes
        );

        var sanitizedData = _sanitizationService.SanitizeAndClassify(rawData);

        if (sanitizedData.ShouldReject)
        {
            return Result.Failure<CustomerResponse>(
                Error.Validation("Lead.Invalid", sanitizedData.RejectionReason ?? "O Lead fornecido não possui informações mínimas válidas após sanitização."));
        }

        // Verifica se e-mail (sanitizado) já existe
        if (sanitizedData.IsEmailValid && !string.IsNullOrWhiteSpace(sanitizedData.Email) && await _repository.EmailExistsAsync(sanitizedData.Email, null, cancellationToken))
        {
            return Result.Failure<CustomerResponse>(
                Error.Conflict("Email", $"Já existe um cadastro com o e-mail '{sanitizedData.Email}'."));
        }

        // Cria a entidade
        var customer = new Customer(
            sanitizedData.Name,
            sanitizedData.Email,
            request.PersonType,
            request.Source);

        // Atualiza informações opcionais
        customer.UpdateBasicInfo(
            sanitizedData.Name,
            sanitizedData.Email,
            request.PersonType,
            sanitizedData.CompanyName,
            request.Document);

        customer.UpdateContactInfo(
            sanitizedData.Phone,
            sanitizedData.WhatsApp,
            request.SecondaryEmail, // secondary email remains untouched for now
            request.Website);

        // Atualiza Status de Qualidade e Flags do Pipeline
        customer.UpdateClassification(
            sanitizedData.Quality,
            sanitizedData.EmailType,
            sanitizedData.HasSuspiciousDomain,
            sanitizedData.IsEligibleForCampaigns);

        customer.UpdateSource(request.Source, request.SourceDetails);
        customer.UpdateNotes(sanitizedData.Notes);
        customer.UpdateTags(request.Tags);

        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Atualiza um customer/lead existente.
    /// </summary>
    public async Task<Result<CustomerResponse>> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer", id));

        // Verifica se e-mail já existe (excluindo o próprio registro)
        if (await _repository.EmailExistsAsync(request.Email, id, cancellationToken))
        {
            return Result.Failure<CustomerResponse>(
                Error.Conflict("Email", $"Já existe outro cadastro com o e-mail '{request.Email}'."));
        }

        // Atualiza a entidade
        customer.UpdateBasicInfo(
            request.Name,
            request.Email,
            request.PersonType,
            request.CompanyName,
            request.Document);

        customer.UpdateContactInfo(
            request.Phone,
            request.WhatsApp,
            request.SecondaryEmail,
            request.Website);

        customer.UpdateSource(request.Source, request.SourceDetails);
        customer.UpdateNotes(request.Notes);
        customer.UpdateTags(request.Tags);

        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Atualiza o status de um customer/lead.
    /// </summary>
    public async Task<Result<CustomerResponse>> UpdateStatusAsync(
        Guid id,
        UpdateCustomerStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer", id));

        customer.UpdateStatus(request.Status);

        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Registra um contato/interação com o customer.
    /// </summary>
    public async Task<Result<CustomerResponse>> RegisterContactAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer", id));

        customer.RegisterContact();

        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Converte um lead para cliente.
    /// </summary>
    public async Task<Result<CustomerResponse>> ConvertToCustomerAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer", id));

        if (customer.IsActiveCustomer)
        {
            return Result.Failure<CustomerResponse>(
                Error.Conflict("Status", "Este registro já é um cliente ativo."));
        }

        customer.ConvertToCustomer();

        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Exclui um customer/lead.
    /// </summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure(Error.NotFound("Customer", id));

        await _repository.DeleteAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Exclui múltiplos customers/leads em uma única operação.
    /// </summary>
    public async Task<Result<BulkDeleteResponse>> BulkDeleteAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return Result.Failure<BulkDeleteResponse>(Error.Validation("Ids", "Nenhum ID informado."));

        var deletedCount = await _repository.BulkDeleteAsync(idList, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new BulkDeleteResponse { DeletedCount = deletedCount });
    }

    /// <summary>
    /// Retorna a timeline de atividades de um customer/lead.
    /// </summary>
    public async Task<Result<IEnumerable<LeadActivityDto>>> GetActivitiesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<IEnumerable<LeadActivityDto>>(Error.NotFound("Customer", id));

        var activities = new List<LeadActivityDto>();

        // Evento de criação
        activities.Add(new LeadActivityDto
        {
            Type = "created",
            Title = "Lead cadastrado",
            Detail = $"Origem: {customer.Source}",
            Date = customer.CreatedAt,
            Status = "info"
        });

        // Último contato registrado
        if (customer.LastContactAt.HasValue)
        {
            activities.Add(new LeadActivityDto
            {
                Type = "contact_registered",
                Title = "Contato registrado",
                Detail = null,
                Date = customer.LastContactAt.Value,
                Status = "info"
            });
        }

        // Conversão para cliente
        if (customer.ConvertedAt.HasValue)
        {
            activities.Add(new LeadActivityDto
            {
                Type = "converted",
                Title = "Convertido em cliente",
                Detail = null,
                Date = customer.ConvertedAt.Value,
                Status = "success"
            });
        }

        // Emails enviados / na fila
        var emailItems = await _emailQueueRepository.GetByCustomerIdAsync(id, cancellationToken);

        foreach (var email in emailItems)
        {
            var (type, title, status, date) = email.Status switch
            {
                EmailQueueStatus.Sent => ("email_sent", "E-mail enviado", "success", email.SentAt ?? email.CreatedAt),
                EmailQueueStatus.Failed => ("email_failed", "Falha no envio do e-mail", "error", email.UpdatedAt ?? email.CreatedAt),
                EmailQueueStatus.Processing => ("email_queued", "E-mail em processamento", "info", email.ProcessingStartedAt ?? email.CreatedAt),
                _ => ("email_queued", "E-mail agendado", "info", email.ScheduledAt)
            };

            activities.Add(new LeadActivityDto
            {
                Type = type,
                Title = title,
                Detail = email.Status == EmailQueueStatus.Failed
                    ? (email.LastError ?? email.Subject)
                    : email.Subject,
                Date = date,
                Status = status,
                WasRead = email.ReadCount > 0 || email.OpenedAt.HasValue,
                ReadAt = email.OpenedAt
            });
        }

        return Result.Success<IEnumerable<LeadActivityDto>>(
            activities.OrderByDescending(a => a.Date).ToList());
    }

    /// <summary>
    /// Executa o processo de sanitização e classificação em lote para a base de leads existente.
    /// </summary>
    public async Task<Result<BulkSanitizationResponse>> SanitizeBaseAsync(
        BulkSanitizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new BulkSanitizationResponse();

        IEnumerable<Customer> targets;
        if (request.CustomerIds != null && request.CustomerIds.Any())
        {
            targets = await _repository.GetByIdsAsync(request.CustomerIds, cancellationToken);
        }
        else
        {
            targets = await _repository.GetAllLeadsAsync(cancellationToken);
        }

        var leadsToUpdate = new List<Customer>();
        var leadsToDelete = new List<Customer>();

        var seenEmails = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
        var seenPhones = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
        var seenCompanies = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);

        // Analisa e deduplica os leads encontrados (mais antigos primeiro para manter o registro inicial como base)
        foreach (var lead in targets.OrderBy(c => c.CreatedAt))
        {
            response.AnalyzedLeads++;

            var rawData = new RawLeadData(lead.Name, lead.Email, lead.Phone, lead.WhatsApp, lead.CompanyName, lead.Notes);
            var sanitized = _sanitizationService.SanitizeAndClassify(rawData);

            bool wasUpdated = false;

            // Rejeição direta (Sem email e sem telefone, ou ser agregador Linktree, ou frase de busca)
            if (sanitized.ShouldReject)
            {
                if (sanitized.RejectionReason != null && sanitized.RejectionReason.Contains("Directory"))
                    response.RemovedByDirectoryOrGeneric++;
                else if (sanitized.RejectionReason != null && sanitized.RejectionReason.Contains("search phrase"))
                    response.RemovedBySearchPhrase++;
                else
                    response.RemovedByInvalidEmail++;

                leadsToDelete.Add(lead);
                continue;
            }

            if (!sanitized.IsEmailValid && !string.IsNullOrWhiteSpace(lead.Email))
            {
                response.RemovedByInvalidEmail++;
                lead.UpdateBasicInfo(lead.Name, null, lead.PersonType, lead.CompanyName, lead.Document);
                wasUpdated = true;
            }

            var activeEmail = sanitized.IsEmailValid ? sanitized.Email : null;
            var activePhone = !string.IsNullOrWhiteSpace(sanitized.Phone) ? sanitized.Phone : sanitized.WhatsApp;
            var activeCompany = !string.IsNullOrWhiteSpace(sanitized.CompanyName) ? sanitized.CompanyName : null;

            // Lógica de Deduplicação e Enriquecimento Inteligente (E-mail -> Telefone -> Nome da Empresa)
            Customer? primaryLead = null;

            if (!string.IsNullOrWhiteSpace(activeEmail) && seenEmails.TryGetValue(activeEmail, out var matchByEmail))
            {
                primaryLead = matchByEmail;
            }
            else if (!string.IsNullOrWhiteSpace(activePhone) && seenPhones.TryGetValue(activePhone, out var matchByPhone))
            {
                primaryLead = matchByPhone;
            }
            else if (!string.IsNullOrWhiteSpace(activeCompany) && seenCompanies.TryGetValue(activeCompany, out var matchByCompany))
            {
                primaryLead = matchByCompany;
            }

            if (primaryLead != null && primaryLead.Id != lead.Id)
            {
                // Mescla as informações no primaryLead
                bool merged = false;

                if (string.IsNullOrWhiteSpace(primaryLead.Phone) && !string.IsNullOrWhiteSpace(sanitized.Phone)) { primaryLead.UpdateContactInfo(sanitized.Phone, primaryLead.WhatsApp, primaryLead.SecondaryEmail, primaryLead.Website); merged = true;}
                if (string.IsNullOrWhiteSpace(primaryLead.WhatsApp) && !string.IsNullOrWhiteSpace(sanitized.WhatsApp)) { primaryLead.UpdateContactInfo(primaryLead.Phone, sanitized.WhatsApp, primaryLead.SecondaryEmail, primaryLead.Website); merged = true;}
                if (string.IsNullOrWhiteSpace(primaryLead.CompanyName) && !string.IsNullOrWhiteSpace(sanitized.CompanyName)) { primaryLead.UpdateBasicInfo(primaryLead.Name, primaryLead.Email, PersonType.Company, sanitized.CompanyName, primaryLead.Document); merged = true; }
                if (string.IsNullOrWhiteSpace(primaryLead.Email) && !string.IsNullOrWhiteSpace(activeEmail)) { primaryLead.UpdateBasicInfo(primaryLead.Name, activeEmail, primaryLead.PersonType, primaryLead.CompanyName, primaryLead.Document); merged = true; }

                // Combina os notes da duplicata
                if (!string.IsNullOrWhiteSpace(sanitized.Notes))
                {
                    var n = primaryLead.Notes + $"\n[Mesclado na Sanitização]: {sanitized.Notes}";
                    primaryLead.UpdateNotes(n);
                    merged = true;
                }

                if (merged && !leadsToUpdate.Contains(primaryLead))
                {
                    leadsToUpdate.Add(primaryLead);
                }

                leadsToDelete.Add(lead);
                response.DuplicatesConsolidated++;
                continue; // Pula o resto da execução pois a entidade duplicada será deletada
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(activeEmail)) seenEmails[activeEmail] = lead;
                if (!string.IsNullOrWhiteSpace(activePhone)) seenPhones[activePhone] = lead;
                if (!string.IsNullOrWhiteSpace(activeCompany)) seenCompanies[activeCompany] = lead;
            }

            // Atualiza os campos do Lead Principal em si com os dados refinados
            if (lead.Name != sanitized.Name || lead.CompanyName != sanitized.CompanyName || lead.Email != activeEmail)
            {
                lead.UpdateBasicInfo(sanitized.Name, activeEmail, string.IsNullOrWhiteSpace(sanitized.CompanyName) ? PersonType.Individual : PersonType.Company, sanitized.CompanyName, lead.Document);
                wasUpdated = true;
            }

            if (lead.Phone != sanitized.Phone || lead.WhatsApp != sanitized.WhatsApp)
            {
                lead.UpdateContactInfo(sanitized.Phone, sanitized.WhatsApp, lead.SecondaryEmail, lead.Website);
                wasUpdated = true;
            }

            if (lead.Notes != sanitized.Notes)
            {
                lead.UpdateNotes(sanitized.Notes);
                wasUpdated = true;
            }

            // Atualiza Classificações
            var currentQuality = lead.Quality;
            var currentEmailType = lead.EmailType;
            var currentSuspicious = lead.HasSuspiciousDomain;
            var currentEligible = lead.IsEligibleForCampaigns;

            // Se ainda assim o domínio for ruim, reporta na estatística e derruba a qualidade drasticamente
            if (sanitized.HasSuspiciousDomain)
            {
                response.RemovedBySuspiciousDomain++; // Ele fica na base, mas reportamos q foi detectado e invocado
            }

            lead.UpdateClassification(sanitized.Quality, sanitized.EmailType, sanitized.HasSuspiciousDomain, sanitized.IsEligibleForCampaigns);

            if (currentQuality != lead.Quality || currentEmailType != lead.EmailType || currentSuspicious != lead.HasSuspiciousDomain || currentEligible != lead.IsEligibleForCampaigns)
            {
                wasUpdated = true;
            }

            if (wasUpdated && !leadsToUpdate.Contains(lead))
            {
                leadsToUpdate.Add(lead);
                response.CorrectedLeads++;
            }
        }

        // Aplica mudanças
        foreach(var update in leadsToUpdate)
        {
            await _repository.UpdateAsync(update, cancellationToken);
        }

        if (leadsToDelete.Any())
        {
            await _repository.BulkDeleteAsync(leadsToDelete.Select(l => l.Id), cancellationToken);
        }

        if (leadsToUpdate.Any() || leadsToDelete.Any())
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        response.ValidLeadsRemaining = response.AnalyzedLeads - leadsToDelete.Count;

        return Result.Success(response);
    }
}
