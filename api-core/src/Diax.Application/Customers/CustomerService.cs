using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
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

    public CustomerService(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        IEmailQueueRepository emailQueueRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _emailQueueRepository = emailQueueRepository;
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
        // Verifica se e-mail já existe
        if (await _repository.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            return Result.Failure<CustomerResponse>(
                Error.Conflict("Email", $"Já existe um cadastro com o e-mail '{request.Email}'."));
        }

        // Cria a entidade
        var customer = new Customer(
            request.Name,
            request.Email,
            request.PersonType,
            request.Source);

        // Atualiza informações opcionais
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
}
