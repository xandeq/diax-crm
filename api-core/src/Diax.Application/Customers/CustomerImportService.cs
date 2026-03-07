using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Application.Customers.Services;
using System.Text.Json;

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

    public CustomerImportService(
        ICustomerRepository customerRepository,
        ICustomerImportRepository importRepository,
        IUnitOfWork unitOfWork,
        ILeadSanitizationService sanitizationService)
    {
        _customerRepository = customerRepository;
        _importRepository = importRepository;
        _unitOfWork = unitOfWork;
        _sanitizationService = sanitizationService;
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

                // Se NÃO existe, CRIAR um NOVO
                if (existingCustomer == null)
                {
                    var customer = new Customer(
                        sanitized.Name,
                        hasEmail ? sanitized.Email : null,
                        string.IsNullOrWhiteSpace(sanitized.CompanyName) ? PersonType.Individual : PersonType.Company,
                        request.Source);

                    customer.UpdateContactInfo(sanitized.Phone, sanitized.WhatsApp);

                    if (!string.IsNullOrWhiteSpace(sanitized.CompanyName))
                    {
                        customer.UpdateBasicInfo(
                            sanitized.Name,
                            hasEmail ? sanitized.Email : null,
                            PersonType.Company,
                            sanitized.CompanyName);
                    }

                    if (!string.IsNullOrWhiteSpace(sanitized.Notes))
                        customer.UpdateNotes(sanitized.Notes);

                    if (!string.IsNullOrWhiteSpace(row.Tags))
                        customer.UpdateTags(row.Tags);

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

                    // Atualiza Telefones se vazio
                    var newPhone = existingCustomer.Phone;
                    var newWhatsApp = existingCustomer.WhatsApp;
                    bool contactUpdated = false;

                    if (string.IsNullOrWhiteSpace(newPhone) && !string.IsNullOrWhiteSpace(sanitized.Phone)) { newPhone = sanitized.Phone; contactUpdated = true; }
                    if (string.IsNullOrWhiteSpace(newWhatsApp) && !string.IsNullOrWhiteSpace(sanitized.WhatsApp)) { newWhatsApp = sanitized.WhatsApp; contactUpdated = true; }

                    if (contactUpdated)
                    {
                        existingCustomer.UpdateContactInfo(newPhone, newWhatsApp, existingCustomer.SecondaryEmail, existingCustomer.Website);
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

                    // Adiciona nas notas os detalhes
                    if (!string.IsNullOrWhiteSpace(sanitized.Notes))
                    {
                        var appendedNotes = existingCustomer.Notes + $"\n [Enriquecimento {DateTime.Now:dd/MM}]: {sanitized.Notes}";
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
                            sanitized.Email ?? sanitized.Name,
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
}
