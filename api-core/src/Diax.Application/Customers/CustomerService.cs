using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Shared.Results;

namespace Diax.Application.Customers;

/// <summary>
/// ServiÃ§o de aplicaÃ§Ã£o para operaÃ§Ãµes com Customers/Leads.
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
    /// ObtÃ©m um customer por ID.
    /// </summary>
    public async Task<Result<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer", id));

        return CustomerResponse.FromEntity(customer);
    }

    /// <summary>
    /// Lista customers com paginaÃ§Ã£o e filtros.
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
                Error.Validation("Lead.Invalid", sanitizedData.RejectionReason ?? "O Lead fornecido nÃ£o possui informaÃ§Ãµes mÃ­nimas vÃ¡lidas apÃ³s sanitizaÃ§Ã£o."));
        }

        Customer? existingCustomer = null;
        if (sanitizedData.IsEmailValid && !string.IsNullOrWhiteSpace(sanitizedData.Email))
        {
            existingCustomer = await _repository.GetByEmailAsync(sanitizedData.Email, cancellationToken);
        }

        if (existingCustomer != null)
        {
            var wasUpdated = EnrichExistingCustomer(existingCustomer, sanitizedData, request);

            if (wasUpdated)
            {
                await _repository.UpdateAsync(existingCustomer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return CustomerResponse.FromEntity(existingCustomer);
        }

        var customer = new Customer(
            sanitizedData.Name,
            sanitizedData.Email,
            request.PersonType,
            request.Source);

        customer.UpdateBasicInfo(
            sanitizedData.Name,
            sanitizedData.Email,
            request.PersonType,
            sanitizedData.CompanyName,
            request.Document);

        customer.UpdateContactInfo(
            sanitizedData.Phone,
            sanitizedData.WhatsApp,
            request.SecondaryEmail,
            request.Website);

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

    private static bool EnrichExistingCustomer(
        Customer existingCustomer,
        SanitizedLeadResult sanitizedData,
        CreateCustomerRequest request)
    {
        var wasUpdated = false;

        var mergedName = existingCustomer.Name;
        var mergedEmail = existingCustomer.Email;
        var mergedPersonType = existingCustomer.PersonType;
        var mergedCompany = existingCustomer.CompanyName;
        var mergedDocument = existingCustomer.Document;

        if (string.IsNullOrWhiteSpace(mergedName) && !string.IsNullOrWhiteSpace(sanitizedData.Name))
        {
            mergedName = sanitizedData.Name;
            wasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(mergedCompany) && !string.IsNullOrWhiteSpace(sanitizedData.CompanyName))
        {
            mergedCompany = sanitizedData.CompanyName;
            mergedPersonType = PersonType.Company;
            wasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(mergedEmail) && sanitizedData.IsEmailValid && !string.IsNullOrWhiteSpace(sanitizedData.Email))
        {
            mergedEmail = sanitizedData.Email;
            wasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(mergedDocument) && !string.IsNullOrWhiteSpace(request.Document))
        {
            mergedDocument = request.Document;
            wasUpdated = true;
        }

        if (wasUpdated)
        {
            existingCustomer.UpdateBasicInfo(mergedName, mergedEmail, mergedPersonType, mergedCompany, mergedDocument);
        }

        var mergedPhone = existingCustomer.Phone;
        var mergedWhatsApp = existingCustomer.WhatsApp;
        var mergedSecondaryEmail = existingCustomer.SecondaryEmail;
        var mergedWebsite = existingCustomer.Website;
        var contactUpdated = false;

        if (string.IsNullOrWhiteSpace(mergedPhone) && !string.IsNullOrWhiteSpace(sanitizedData.Phone))
        {
            mergedPhone = sanitizedData.Phone;
            contactUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(mergedWhatsApp) && !string.IsNullOrWhiteSpace(sanitizedData.WhatsApp))
        {
            mergedWhatsApp = sanitizedData.WhatsApp;
            contactUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(mergedSecondaryEmail) && !string.IsNullOrWhiteSpace(request.SecondaryEmail))
        {
            mergedSecondaryEmail = request.SecondaryEmail;
            contactUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(mergedWebsite) && !string.IsNullOrWhiteSpace(request.Website))
        {
            mergedWebsite = request.Website;
            contactUpdated = true;
        }

        if (contactUpdated)
        {
            existingCustomer.UpdateContactInfo(mergedPhone, mergedWhatsApp, mergedSecondaryEmail, mergedWebsite);
            wasUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.SourceDetails) && string.IsNullOrWhiteSpace(existingCustomer.SourceDetails))
        {
            existingCustomer.UpdateSource(request.Source, request.SourceDetails);
            wasUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(sanitizedData.Notes))
        {
            var currentNotes = existingCustomer.Notes ?? string.Empty;
            if (!currentNotes.Contains(sanitizedData.Notes, StringComparison.OrdinalIgnoreCase))
            {
                var mergedNotes = string.IsNullOrWhiteSpace(currentNotes)
                    ? sanitizedData.Notes
                    : $"{currentNotes}\n[Enriquecimento]: {sanitizedData.Notes}";
                existingCustomer.UpdateNotes(mergedNotes);
                wasUpdated = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            var currentTags = existingCustomer.Tags ?? string.Empty;
            if (!currentTags.Contains(request.Tags, StringComparison.OrdinalIgnoreCase))
            {
                var mergedTags = string.IsNullOrWhiteSpace(currentTags)
                    ? request.Tags
                    : $"{currentTags},{request.Tags}";
                existingCustomer.UpdateTags(mergedTags);
                wasUpdated = true;
            }
        }

        existingCustomer.UpdateClassification(
            existingCustomer.Quality ?? sanitizedData.Quality,
            existingCustomer.EmailType ?? sanitizedData.EmailType,
            existingCustomer.HasSuspiciousDomain || sanitizedData.HasSuspiciousDomain,
            existingCustomer.IsEligibleForCampaigns || sanitizedData.IsEligibleForCampaigns);

        return wasUpdated;
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

        if (await _repository.EmailExistsAsync(request.Email, id, cancellationToken))
        {
            return Result.Failure<CustomerResponse>(
                Error.Conflict("Email", $"JÃ¡ existe outro cadastro com o e-mail '{request.Email}'."));
        }

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
    /// Registra um contato/interaÃ§Ã£o com o customer.
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
                Error.Conflict("Status", "Este registro jÃ¡ Ã© um cliente ativo."));
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
    /// Exclui mÃºltiplos customers/leads em uma Ãºnica operaÃ§Ã£o.
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

        activities.Add(new LeadActivityDto
        {
            Type = "created",
            Title = "Lead cadastrado",
            Detail = $"Origem: {customer.Source}",
            Date = customer.CreatedAt,
            Status = "info"
        });

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
    /// Executa o processo de sanitizaÃ§Ã£o e classificaÃ§Ã£o em lote para a base de leads existente.
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

        foreach (var lead in targets.OrderBy(c => c.CreatedAt))
        {
            response.AnalyzedLeads++;

            var rawData = new RawLeadData(lead.Name, lead.Email, lead.Phone, lead.WhatsApp, lead.CompanyName, lead.Notes);
            var sanitized = _sanitizationService.SanitizeAndClassify(rawData);

            bool wasUpdated = false;

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
                bool merged = false;

                if (string.IsNullOrWhiteSpace(primaryLead.Phone) && !string.IsNullOrWhiteSpace(sanitized.Phone)) { primaryLead.UpdateContactInfo(sanitized.Phone, primaryLead.WhatsApp, primaryLead.SecondaryEmail, primaryLead.Website); merged = true; }
                if (string.IsNullOrWhiteSpace(primaryLead.WhatsApp) && !string.IsNullOrWhiteSpace(sanitized.WhatsApp)) { primaryLead.UpdateContactInfo(primaryLead.Phone, sanitized.WhatsApp, primaryLead.SecondaryEmail, primaryLead.Website); merged = true; }
                if (string.IsNullOrWhiteSpace(primaryLead.CompanyName) && !string.IsNullOrWhiteSpace(sanitized.CompanyName)) { primaryLead.UpdateBasicInfo(primaryLead.Name, primaryLead.Email, PersonType.Company, sanitized.CompanyName, primaryLead.Document); merged = true; }
                if (string.IsNullOrWhiteSpace(primaryLead.Email) && !string.IsNullOrWhiteSpace(activeEmail)) { primaryLead.UpdateBasicInfo(primaryLead.Name, activeEmail, primaryLead.PersonType, primaryLead.CompanyName, primaryLead.Document); merged = true; }

                if (!string.IsNullOrWhiteSpace(sanitized.Notes))
                {
                    var n = primaryLead.Notes + $"\n[Mesclado na SanitizaÃ§Ã£o]: {sanitized.Notes}";
                    primaryLead.UpdateNotes(n);
                    merged = true;
                }

                if (merged && !leadsToUpdate.Contains(primaryLead))
                {
                    leadsToUpdate.Add(primaryLead);
                }

                leadsToDelete.Add(lead);
                response.DuplicatesConsolidated++;
                continue;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(activeEmail)) seenEmails[activeEmail] = lead;
                if (!string.IsNullOrWhiteSpace(activePhone)) seenPhones[activePhone] = lead;
                if (!string.IsNullOrWhiteSpace(activeCompany)) seenCompanies[activeCompany] = lead;
            }

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

            var currentQuality = lead.Quality;
            var currentEmailType = lead.EmailType;
            var currentSuspicious = lead.HasSuspiciousDomain;
            var currentEligible = lead.IsEligibleForCampaigns;

            if (sanitized.HasSuspiciousDomain)
            {
                response.RemovedBySuspiciousDomain++;
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

        foreach (var update in leadsToUpdate)
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
