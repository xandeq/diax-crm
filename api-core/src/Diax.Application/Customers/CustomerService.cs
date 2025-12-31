using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Results;

namespace Diax.Application.Customers;

/// <summary>
/// Serviço de aplicação para operações com Customers/Leads.
/// </summary>
public class CustomerService : IApplicationService
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
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
        // Ajusta filtros baseado em flags
        CustomerStatus? statusFilter = request.Status;

        // NOTA: Removemos o filtro estrito de OnlyCustomers para permitir que registros
        // criados recentemente (que nascem como Leads) apareçam na listagem de Clientes.
        // if (request.OnlyCustomers == true)
        // {
        //     statusFilter = CustomerStatus.Customer;
        // }

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            statusFilter,
            request.Source,
            cancellationToken);

        // Se OnlyLeads, filtra adicionalmente
        if (request.OnlyLeads == true)
        {
            items = items.Where(c => c.IsLead);
            totalCount = items.Count();
        }
        
        // OnlyCustomers agora retorna tudo (comportamento "All") se não houver status específico

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
}
