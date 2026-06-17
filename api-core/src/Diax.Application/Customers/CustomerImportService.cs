using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Application.Customers.Services;
using System.Text.Json;
using System.Linq;
using Diax.Domain.EmailMarketing;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Application.EmailMarketing;

namespace Diax.Application.Customers;

/// <summary>
/// Serviço de aplicação para importação em lote de customers/leads.
/// </summary>
public class CustomerImportService : IApplicationService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerImportRepository _importRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILeadSanitizationService _sanitizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPilotCircuitBreaker _circuitBreaker;

    public CustomerImportService(
        ICustomerRepository customerRepository,
        ICustomerImportRepository importRepository,
        IUnitOfWork unitOfWork,
        ILeadSanitizationService sanitizationService,
        ICurrentUserService currentUserService,
        IEmailSuppressionRepository suppressionRepository,
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        IPilotCircuitBreaker circuitBreaker)
    {
        _customerRepository = customerRepository;
        _importRepository = importRepository;
        _unitOfWork = unitOfWork;
        _sanitizationService = sanitizationService;
        _currentUserService = currentUserService;
        _suppressionRepository = suppressionRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _circuitBreaker = circuitBreaker;
    }

    /// <summary>
    /// Importa uma lista de customers/leads em lote.
    /// </summary>
    /// <param name="request">Request com lista de customers a importar</param>
    /// <param name="fileName">Nome do arquivo ou identificador da importação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Response com contadores de sucesso/falha e lista de erros</returns>
    public async Task<BulkImportResponse> ImportAsync(
        BulkImportRequest request,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // 1. Limite de 10 leads para importação outbound real
        if (request.Source == LeadSource.Import && !request.DryRun && request.Customers.Count > 10)
        {
            var limitError = new ImportError(0, "", "O lote de importação excede o limite máximo de 10 leads do piloto controlado.");
            await LogPilotEventAsync("PilotImportBlocked", "Failed", null, request.Customers.Count, false, "O lote de importação excede o limite máximo de 10 leads do piloto controlado.", cancellationToken);
            return new BulkImportResponse(
                false,
                request.Customers.Count,
                0,
                1,
                0,
                new List<ImportError> { limitError });
        }

        if (request.Source == LeadSource.Import && request.DryRun)
        {
            await LogPilotEventAsync("PilotDryRunStarted", "Success", null, request.Customers.Count, true, null, cancellationToken);
        }

        // Se for DryRun ou se for importação manual (LeadSource.Import), fazemos o passo de validação primeiro
        if (request.DryRun || request.Source == LeadSource.Import)
        {
            var validationErrors = new List<ImportError>();
            var valSkippedCount = 0;
            var valSeenEmailsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var valSeenPhonesInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < request.Customers.Count; i++)
            {
                var row = request.Customers[i];
                try
                {
                    // A. Rejeitar sem origem
                    if (request.Source == LeadSource.Unknown)
                    {
                        validationErrors.Add(new ImportError(
                            i + 1,
                            row.Email ?? row.Name ?? "",
                            "Origem inválida ou não especificada. É obrigatório informar uma origem válida."));
                        continue;
                    }

                    // B. Rejeitar e-mail inválido
                    if (string.IsNullOrWhiteSpace(row.Email) || !IsValidEmail(row.Email))
                    {
                        validationErrors.Add(new ImportError(
                            i + 1,
                            row.Email ?? row.Name ?? "",
                            "E-mail inválido ou ausente. Para campanhas frias, todos os leads precisam possuir e-mail válido."));
                        continue;
                    }

                    // C. Exigir validation_status adequado (apenas para importações de listas frias/outbound)
                    if (request.Source == LeadSource.Import)
                    {
                        if (string.IsNullOrWhiteSpace(row.ValidationStatus))
                        {
                            validationErrors.Add(new ImportError(
                                i + 1,
                                row.Email,
                                "Validation status é obrigatório e deve ser um status válido (ex: 'valido', 'verificado')."));
                            continue;
                        }

                        var statusLower = row.ValidationStatus.Trim().ToLowerInvariant();
                        if (statusLower.Contains("invalid") || statusLower.Contains("inválido") || 
                            statusLower.Contains("unknown") || statusLower.Contains("desconhecido") || 
                            statusLower.Contains("disposable") || statusLower.Contains("descartável") ||
                            statusLower.Contains("catch-all") || statusLower.Contains("catchall") ||
                            statusLower.Contains("bounce"))
                        {
                            validationErrors.Add(new ImportError(
                                i + 1,
                                row.Email,
                                $"Validation status inadequado: '{row.ValidationStatus}'."));
                            continue;
                        }
                    }

                    // D. Exigir consent_status (apenas para importações de listas frias/outbound)
                    if (request.Source == LeadSource.Import)
                    {
                        if (string.IsNullOrWhiteSpace(row.ConsentStatus))
                        {
                            validationErrors.Add(new ImportError(
                                i + 1,
                                row.Email,
                                "Consent status é obrigatório para importação outbound."));
                            continue;
                        }

                        var consentLower = row.ConsentStatus.Trim().ToLowerInvariant();
                        if (consentLower.Contains("recusado") || 
                            consentLower.Contains("não consentido") || 
                            consentLower.Contains("nao consentido") || 
                            consentLower.Contains("opt-out") || 
                            consentLower.Contains("optout"))
                        {
                            validationErrors.Add(new ImportError(
                                i + 1,
                                row.Email,
                                "Consent status inválido ou recusado."));
                            continue;
                        }
                    }

                    // 1. Aplica o SanitizationService na linha
                    var rawData = new RawLeadData(
                        row.Name ?? "",
                        row.Email,
                        row.Phone,
                        row.WhatsApp,
                        row.CompanyName,
                        row.Notes
                    );

                    var sanitized = _sanitizationService.SanitizeAndClassify(rawData);

                    // Rejeitar se nome for inválido ou muito curto
                    if (sanitized.ShouldReject)
                    {
                        validationErrors.Add(new ImportError(
                            i + 1,
                            row.Email ?? row.Name ?? "",
                            sanitized.RejectionReason ?? "Dados inválidos"));
                        continue;
                    }

                    var hasEmail = sanitized.IsEmailValid && !string.IsNullOrWhiteSpace(sanitized.Email);
                    var primaryPhone = sanitized.WhatsApp ?? sanitized.Phone;

                    Customer? existingCustomer = null;

                    if (hasEmail)
                    {
                        // Dedup na memória
                        if (!valSeenEmailsInBatch.Add(sanitized.Email!))
                        {
                            valSkippedCount++;
                            validationErrors.Add(new ImportError(
                                i + 1,
                                sanitized.Email!,
                                $"Duplicata no lote: email '{sanitized.Email}' aparece mais de uma vez"));
                            continue;
                        }

                        // Busca customer existente no DB
                        existingCustomer = await _customerRepository.GetByEmailAsync(sanitized.Email!, cancellationToken);
                    }
                    else if (primaryPhone != null)
                    {
                        // Dedup na memória pelo telefone
                        if (!valSeenPhonesInBatch.Add(primaryPhone))
                        {
                            valSkippedCount++;
                            validationErrors.Add(new ImportError(
                                i + 1,
                                sanitized.Name,
                                $"Duplicata no lote: telefone '{primaryPhone}' aparece mais de uma vez"));
                            continue;
                        }

                        // Busca customer existente pelo telefone
                        existingCustomer = await _customerRepository.GetByPhoneAsync(primaryPhone, cancellationToken);
                    }

                    // E. Rejeitar opt-out ou bounced do banco/supressão
                    if (existingCustomer != null && existingCustomer.EmailOptOut)
                    {
                        validationErrors.Add(new ImportError(
                            i + 1,
                            row.Email,
                            "Lead rejeitado: Contato possui opt-out ativo."));
                        continue;
                    }

                    // Para o piloto, se o contato já existe, rejeitar duplicata
                    if (request.Source == LeadSource.Import && existingCustomer != null)
                    {
                        validationErrors.Add(new ImportError(
                            i + 1,
                            row.Email,
                            $"Lead duplicado: O e-mail '{row.Email}' já está cadastrado no sistema."));
                        continue;
                    }

                    if (_currentUserService.UserId.HasValue)
                    {
                        var isSuppressed = await _suppressionRepository.IsSuppressedAsync(
                            _currentUserService.UserId.Value,
                            row.Email.Trim().ToLowerInvariant(),
                            cancellationToken);

                        if (isSuppressed)
                        {
                            validationErrors.Add(new ImportError(
                                i + 1,
                                row.Email,
                                "Lead rejeitado: E-mail na lista de supressão (unsubscribed ou bounced)."));
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    validationErrors.Add(new ImportError(
                        i + 1,
                        row.Email ?? "",
                        $"Erro ao processar: {ex.Message}"));
                }
            }

            // Se for DryRun, ou se houver qualquer erro/skip na validação da importação real de LeadSource.Import, retornamos e NÃO persistimos
            if (request.DryRun || validationErrors.Any())
            {
                var isSuccess = !validationErrors.Any();
                var total = request.Customers.Count;
                var failed = validationErrors.Count - valSkippedCount;
                var successed = total - validationErrors.Count;

                if (request.Source == LeadSource.Import)
                {
                    if (request.DryRun)
                    {
                        if (validationErrors.Any())
                        {
                            await LogPilotEventAsync("PilotDryRunFailed", "Failed", null, total, true, $"Erros de validação: {string.Join(" | ", validationErrors.Select(e => e.ErrorMessage))}", cancellationToken);
                        }
                        else
                        {
                            await LogPilotEventAsync("PilotDryRunCompleted", "Success", null, total, true, null, cancellationToken);
                        }
                    }
                    else if (validationErrors.Any())
                    {
                        await LogPilotEventAsync("PilotImportBlocked", "Failed", null, total, false, $"Importação real bloqueada devido a erros de validação: {string.Join(" | ", validationErrors.Select(e => e.ErrorMessage))}", cancellationToken);
                    }
                }

                return new BulkImportResponse(
                    isSuccess,
                    total,
                    successed,
                    failed,
                    valSkippedCount,
                    validationErrors);
            }
        }

        // Cria registro de importação
        var import = new CustomerImport(fileName, ImportType.CSV, request.Customers.Count);
        await _importRepository.AddAsync(import, cancellationToken);

        var errors = new List<ImportError>();
        var successCount = 0;
        var skippedCount = 0;

        // HashSets para dedup dentro do próprio lote (evita duplicatas na mesma importação)
        var seenEmailsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenPhonesInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Processa cada linha
        for (int i = 0; i < request.Customers.Count; i++)
        {
            var row = request.Customers[i];

            try
            {
                // A. Rejeitar sem origem
                if (request.Source == LeadSource.Unknown)
                {
                    errors.Add(new ImportError(
                        i + 1,
                        row.Email ?? row.Name ?? "",
                        "Origem inválida ou não especificada. É obrigatório informar uma origem válida."));
                    continue;
                }

                // B. Rejeitar e-mail inválido
                if (string.IsNullOrWhiteSpace(row.Email) || !IsValidEmail(row.Email))
                {
                    errors.Add(new ImportError(
                        i + 1,
                        row.Email ?? row.Name ?? "",
                        "E-mail inválido ou ausente. Para campanhas frias, todos os leads precisam possuir e-mail válido."));
                    continue;
                }

                // C. Exigir validation_status adequado (apenas para importações de listas frias/outbound)
                if (request.Source == LeadSource.Import)
                {
                    if (string.IsNullOrWhiteSpace(row.ValidationStatus))
                    {
                        errors.Add(new ImportError(
                            i + 1,
                            row.Email,
                            "Validation status é obrigatório e deve ser um status válido (ex: 'valido', 'verificado')."));
                        continue;
                    }

                    var statusLower = row.ValidationStatus.Trim().ToLowerInvariant();
                    if (statusLower.Contains("invalid") || statusLower.Contains("inválido") || 
                        statusLower.Contains("unknown") || statusLower.Contains("desconhecido") || 
                        statusLower.Contains("disposable") || statusLower.Contains("descartável") ||
                        statusLower.Contains("catch-all") || statusLower.Contains("catchall") ||
                        statusLower.Contains("bounce"))
                    {
                        errors.Add(new ImportError(
                            i + 1,
                            row.Email,
                            $"Validation status inadequado: '{row.ValidationStatus}'."));
                        continue;
                    }
                }

                // 1. Aplica o SanitizationService na linha
                var rawData = new RawLeadData(
                    row.Name ?? "",
                    row.Email,
                    row.Phone,
                    row.WhatsApp,
                    row.CompanyName,
                    row.Notes
                );

                var sanitized = _sanitizationService.SanitizeAndClassify(rawData);

                // Rejeitar se nome for inválido ou muito curto
                if (sanitized.ShouldReject)
                {
                    errors.Add(new ImportError(
                        i + 1,
                        row.Email ?? row.Name ?? "",
                        sanitized.RejectionReason ?? "Dados inválidos"));
                    continue;
                }

                var hasEmail = sanitized.IsEmailValid && !string.IsNullOrWhiteSpace(sanitized.Email);
                var primaryPhone = sanitized.WhatsApp ?? sanitized.Phone;

                Customer? existingCustomer = null;

                if (hasEmail)
                {
                    // Dedup na memória
                    if (!seenEmailsInBatch.Add(sanitized.Email!))
                    {
                        skippedCount++;
                        errors.Add(new ImportError(
                            i + 1,
                            sanitized.Email!,
                            $"Duplicata no lote: email '{sanitized.Email}' aparece mais de uma vez"));
                        continue;
                    }

                    // Busca customer existente no DB para enriquecimento
                    existingCustomer = await _customerRepository.GetByEmailAsync(sanitized.Email!, cancellationToken);
                }
                else if (primaryPhone != null)
                {
                    // Dedup na memória pelo telefone
                    if (!seenPhonesInBatch.Add(primaryPhone))
                    {
                        skippedCount++;
                        errors.Add(new ImportError(
                            i + 1,
                            sanitized.Name,
                            $"Duplicata no lote: telefone '{primaryPhone}' aparece mais de uma vez"));
                        continue;
                    }

                    // Busca customer existente pelo telefone
                    existingCustomer = await _customerRepository.GetByPhoneAsync(primaryPhone, cancellationToken);
                }

                // D. Rejeitar opt-out ou bounced do banco/supressão
                if (existingCustomer != null && existingCustomer.EmailOptOut)
                {
                    errors.Add(new ImportError(
                        i + 1,
                        row.Email,
                        "Lead rejeitado: Contato possui opt-out ativo."));
                    continue;
                }

                if (_currentUserService.UserId.HasValue)
                {
                    var isSuppressed = await _suppressionRepository.IsSuppressedAsync(
                        _currentUserService.UserId.Value,
                        row.Email.Trim().ToLowerInvariant(),
                        cancellationToken);

                    if (isSuppressed)
                    {
                        errors.Add(new ImportError(
                            i + 1,
                            row.Email,
                            "Lead rejeitado: E-mail na lista de supressão (unsubscribed ou bounced)."));
                        continue;
                    }
                }

                // Se NÃO existe, CRIAR um NOVO
                if (existingCustomer == null)
                {
                    var customer = new Customer(
                        sanitized.Name,
                        hasEmail ? sanitized.Email : null,
                        string.IsNullOrWhiteSpace(sanitized.CompanyName) ? PersonType.Individual : PersonType.Company,
                        request.Source);

                    customer.UpdateContactInfo(sanitized.Phone, sanitized.WhatsApp, website: row.Website);

                    if (!string.IsNullOrWhiteSpace(sanitized.CompanyName))
                    {
                        customer.UpdateBasicInfo(
                            sanitized.Name,
                            hasEmail ? sanitized.Email : null,
                            PersonType.Company,
                            sanitized.CompanyName);
                    }

                    // Build tags
                    var tagsList = new List<string> { "pilot_candidate" };
                    if (!string.IsNullOrWhiteSpace(row.Tags))
                    {
                        tagsList.AddRange(row.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()));
                    }

                    // Map CurrentTool to tag
                    if (!string.IsNullOrWhiteSpace(row.CurrentTool))
                    {
                        var tool = row.CurrentTool.Trim().ToLowerInvariant();
                        if (tool.Contains("pipedrive")) tagsList.Add("pipedrive");
                        else if (tool.Contains("rd station") || tool.Contains("rdstation")) tagsList.Add("rd station");
                        else if (tool.Contains("notion")) tagsList.Add("notion");
                        else if (tool.Contains("planilha") || tool.Contains("sheets")) tagsList.Add("planilha");
                    }

                    // Map MainPain to tag
                    if (!string.IsNullOrWhiteSpace(row.MainPain))
                    {
                        var pain = row.MainPain.Trim().ToLowerInvariant();
                        if (pain.Contains("whatsapp") || pain.Contains("whats")) tagsList.Add("whatsapp");
                        else if (pain.Contains("financeiro") || pain.Contains("cobranca") || pain.Contains("cobrança")) tagsList.Add("financeiro");
                    }

                    // Map ValidationStatus and ConsentStatus to tags
                    if (!string.IsNullOrWhiteSpace(row.ValidationStatus))
                    {
                        tagsList.Add($"validation_status_{row.ValidationStatus.Trim().ToLowerInvariant().Replace(" ", "_")}");
                    }
                    if (!string.IsNullOrWhiteSpace(row.ConsentStatus))
                    {
                        tagsList.Add($"consent_status_{row.ConsentStatus.Trim().ToLowerInvariant().Replace(" ", "_")}");
                    }

                    var finalTags = string.Join(", ", tagsList.Distinct(StringComparer.OrdinalIgnoreCase));
                    customer.UpdateTags(finalTags);

                    // Build notes
                    var extraNotes = new List<string>();
                    if (!string.IsNullOrWhiteSpace(sanitized.Notes)) extraNotes.Add(sanitized.Notes);
                    if (!string.IsNullOrWhiteSpace(row.City)) extraNotes.Add($"Cidade: {row.City}");
                    if (!string.IsNullOrWhiteSpace(row.ValidationStatus)) extraNotes.Add($"Validation Status: {row.ValidationStatus}");
                    if (!string.IsNullOrWhiteSpace(row.ConsentStatus)) extraNotes.Add($"Consent Status: {row.ConsentStatus}");
                    var finalNotes = string.Join("\n", extraNotes);
                    customer.UpdateNotes(finalNotes);

                    customer.UpdateClassification(
                        sanitized.Quality,
                        sanitized.EmailType,
                        sanitized.HasSuspiciousDomain,
                        sanitized.IsEligibleForCampaigns);

                    await _customerRepository.AddAsync(customer, cancellationToken);
                    successCount++;
                }
                else
                {
                    // Se EXISTE, ENRIQUECER OS DADOS
                    bool wasUpdated = false;

                    // Atualiza Nome e Empresa se estiverem vazios no banco e vieram preenchidos
                    var newName = existingCustomer.Name;
                    var newCompany = existingCustomer.CompanyName;
                    var newType = existingCustomer.PersonType;

                    if (string.IsNullOrWhiteSpace(existingCustomer.Name) && !string.IsNullOrWhiteSpace(sanitized.Name)) { newName = sanitized.Name; wasUpdated = true; }
                    if (string.IsNullOrWhiteSpace(existingCustomer.CompanyName) && !string.IsNullOrWhiteSpace(sanitized.CompanyName))
                    {
                        newCompany = sanitized.CompanyName;
                        newType = PersonType.Company;
                        wasUpdated = true;
                    }

                    if (wasUpdated)
                    {
                        existingCustomer.UpdateBasicInfo(newName, existingCustomer.Email, newType, newCompany, existingCustomer.Document);
                    }

                    // Atualiza Telefones e Website se vazio
                    var newPhone = existingCustomer.Phone;
                    var newWhatsApp = existingCustomer.WhatsApp;
                    var newWebsite = existingCustomer.Website ?? row.Website;
                    bool contactUpdated = false;

                    if (string.IsNullOrWhiteSpace(newPhone) && !string.IsNullOrWhiteSpace(sanitized.Phone)) { newPhone = sanitized.Phone; contactUpdated = true; }
                    if (string.IsNullOrWhiteSpace(newWhatsApp) && !string.IsNullOrWhiteSpace(sanitized.WhatsApp)) { newWhatsApp = sanitized.WhatsApp; contactUpdated = true; }
                    if (string.IsNullOrWhiteSpace(existingCustomer.Website) && !string.IsNullOrWhiteSpace(row.Website)) { newWebsite = row.Website; contactUpdated = true; }

                    if (contactUpdated || newWebsite != existingCustomer.Website)
                    {
                        existingCustomer.UpdateContactInfo(newPhone, newWhatsApp, existingCustomer.SecondaryEmail, newWebsite);
                        wasUpdated = true;
                    }

                    // Avaliação reclassificada e mesclada
                    var newQuality = existingCustomer.Quality ?? sanitized.Quality;
                    var newEmailType = existingCustomer.EmailType ?? sanitized.EmailType;

                    existingCustomer.UpdateClassification(
                        newQuality,
                        newEmailType,
                        existingCustomer.HasSuspiciousDomain || sanitized.HasSuspiciousDomain, // Mantem flag se algum tiver
                        existingCustomer.IsEligibleForCampaigns && sanitized.IsEligibleForCampaigns // Mantem falso se qualquer um alertou
                    );

                    // Build tags for existing customer
                    var existingTagsList = new List<string> { "pilot_candidate" };
                    if (!string.IsNullOrWhiteSpace(existingCustomer.Tags))
                    {
                        existingTagsList.AddRange(existingCustomer.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()));
                    }
                    if (!string.IsNullOrWhiteSpace(row.Tags))
                    {
                        existingTagsList.AddRange(row.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()));
                    }

                    // Map CurrentTool to tag
                    if (!string.IsNullOrWhiteSpace(row.CurrentTool))
                    {
                        var tool = row.CurrentTool.Trim().ToLowerInvariant();
                        if (tool.Contains("pipedrive")) existingTagsList.Add("pipedrive");
                        else if (tool.Contains("rd station") || tool.Contains("rdstation")) existingTagsList.Add("rd station");
                        else if (tool.Contains("notion")) existingTagsList.Add("notion");
                        else if (tool.Contains("planilha") || tool.Contains("sheets")) existingTagsList.Add("planilha");
                    }

                    // Map MainPain to tag
                    if (!string.IsNullOrWhiteSpace(row.MainPain))
                    {
                        var pain = row.MainPain.Trim().ToLowerInvariant();
                        if (pain.Contains("whatsapp") || pain.Contains("whats")) existingTagsList.Add("whatsapp");
                        else if (pain.Contains("financeiro") || pain.Contains("cobranca") || pain.Contains("cobrança")) existingTagsList.Add("financeiro");
                    }

                    // Map ValidationStatus and ConsentStatus to tags
                    if (!string.IsNullOrWhiteSpace(row.ValidationStatus))
                    {
                        existingTagsList.Add($"validation_status_{row.ValidationStatus.Trim().ToLowerInvariant().Replace(" ", "_")}");
                    }
                    if (!string.IsNullOrWhiteSpace(row.ConsentStatus))
                    {
                        existingTagsList.Add($"consent_status_{row.ConsentStatus.Trim().ToLowerInvariant().Replace(" ", "_")}");
                    }

                    var finalTags = string.Join(", ", existingTagsList.Distinct(StringComparer.OrdinalIgnoreCase));
                    if (finalTags != existingCustomer.Tags)
                    {
                        existingCustomer.UpdateTags(finalTags);
                        wasUpdated = true;
                    }

                    // Adiciona nas notas os detalhes
                    var extraNotes = new List<string>();
                    if (!string.IsNullOrWhiteSpace(sanitized.Notes)) extraNotes.Add(sanitized.Notes);
                    if (!string.IsNullOrWhiteSpace(row.City)) extraNotes.Add($"Cidade: {row.City}");
                    if (!string.IsNullOrWhiteSpace(row.ValidationStatus)) extraNotes.Add($"Validation Status: {row.ValidationStatus}");
                    if (!string.IsNullOrWhiteSpace(row.ConsentStatus)) extraNotes.Add($"Consent Status: {row.ConsentStatus}");

                    if (extraNotes.Any())
                    {
                        var appendedNotes = existingCustomer.Notes + $"\n [Enriquecimento {DateTime.Now:dd/MM}]: " + string.Join(" | ", extraNotes);
                        existingCustomer.UpdateNotes(appendedNotes);
                        wasUpdated = true;
                    }

                    if (wasUpdated)
                    {
                        await _customerRepository.UpdateAsync(existingCustomer, cancellationToken);
                        successCount++;
                    }
                    else
                    {
                        skippedCount++;
                        errors.Add(new ImportError(
                            i + 1,
                            sanitized.Email ?? row.Email,
                            "Duplicata ignorada (nenhuma informação nova para enriquecer)."));
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError(
                    i + 1,
                    row.Email ?? "",
                    $"Erro ao processar: {ex.Message}"));
            }
        }

        // Atualiza o registro de importação com os resultados
        var errorJson = errors.Any()
            ? JsonSerializer.Serialize(errors)
            : null;

        import.Complete(successCount, errors.Count, errorJson);

        // Salva tudo no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.Source == LeadSource.Import)
        {
            await LogPilotEventAsync("PilotImportCompleted", "Success", null, successCount, false, null, cancellationToken);
        }

        return new BulkImportResponse(
            successCount > 0,
            request.Customers.Count,
            successCount,
            errors.Count - skippedCount,
            skippedCount,
            errors);
    }

    /// <summary>
    /// Obtém o histórico de importações paginado.
    /// </summary>
    /// <param name="page">Número da página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Response paginado com histórico de importações</returns>
    public async Task<PagedResponse<ImportHistoryResponse>> GetImportHistoryAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _importRepository.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

        var responses = items.Select(ImportHistoryResponse.FromEntity);

        return PagedResponse<ImportHistoryResponse>.Create(
            responses,
            page,
            pageSize,
            totalCount);
    }

    /// <summary>
    /// Valida se um email tem formato válido (básico).
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Limpa e normaliza número de telefone, removendo caracteres extras.
    /// </summary>
    private static string? CleanPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        // Remove espaços duplos, :: e outros caracteres estranhos
        phone = phone.Trim()
            .Replace("::", "")
            .Replace("  ", " ")
            .Trim();

        // Limita a 50 caracteres (tamanho máximo da coluna)
        if (phone.Length > 50)
            phone = phone.Substring(0, 50).Trim();

        return string.IsNullOrWhiteSpace(phone) ? null : phone;
    }

    private async Task LogPilotEventAsync(
        string action,
        string result,
        Guid? campaignId,
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
            campaignId?.ToString() ?? string.Empty,
            $"Pilot campaign event: {action} ({result})",
            newValues: JsonSerializer.Serialize(details)
        );

        await _auditLogRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
