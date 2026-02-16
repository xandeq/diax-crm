using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
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

    public CustomerImportService(
        ICustomerRepository customerRepository,
        ICustomerImportRepository importRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _importRepository = importRepository;
        _unitOfWork = unitOfWork;
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

        // Processa cada linha
        for (int i = 0; i < request.Customers.Count; i++)
        {
            var row = request.Customers[i];

            try
            {
                // Valida campo obrigatório: Nome
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    errors.Add(new ImportError(
                        i + 1,
                        row.Email ?? row.Name ?? "",
                        "Nome é obrigatório"));
                    continue;
                }

                // Email é opcional, mas se fornecido deve ser válido
                var hasEmail = !string.IsNullOrWhiteSpace(row.Email);

                if (hasEmail)
                {
                    // Valida formato de email
                    if (!IsValidEmail(row.Email))
                    {
                        errors.Add(new ImportError(
                            i + 1,
                            row.Name,
                            $"Formato de email inválido: '{row.Email}'"));
                        continue;
                    }

                    // Verifica se email já existe
                    if (await _customerRepository.EmailExistsAsync(row.Email, null, cancellationToken))
                    {
                        errors.Add(new ImportError(
                            i + 1,
                            row.Email,
                            "Email já existe no sistema"));
                        continue;
                    }
                }

                // Cria o customer (usa email genérico se não fornecido)
                var emailToUse = hasEmail ? row.Email : $"sem-email-{Guid.NewGuid():N}@placeholder.local";

                var customer = new Customer(
                    row.Name,
                    emailToUse,
                    PersonType.Individual,
                    request.Source);

                // Atualiza informações de contato
                customer.UpdateContactInfo(row.Phone, row.WhatsApp);

                // Se tem CompanyName, assume que é Pessoa Jurídica
                if (!string.IsNullOrWhiteSpace(row.CompanyName))
                {
                    customer.UpdateBasicInfo(
                        row.Name,
                        row.Email,
                        PersonType.Company,
                        row.CompanyName);
                }

                // Atualiza notes e tags se fornecidos
                if (!string.IsNullOrWhiteSpace(row.Notes))
                {
                    customer.UpdateNotes(row.Notes);
                }

                if (!string.IsNullOrWhiteSpace(row.Tags))
                {
                    customer.UpdateTags(row.Tags);
                }

                // Adiciona ao repositório
                await _customerRepository.AddAsync(customer, cancellationToken);
                successCount++;
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
            errors.Count,
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
}
