using Diax.Domain.Common;
using Diax.Domain.Customers.Enums;

namespace Diax.Domain.Customers;

/// <summary>
/// Repositório específico para Customer com operações adicionais.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>
    /// Busca cliente por e-mail.
    /// </summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca cliente por documento (CPF/CNPJ).
    /// </summary>
    Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista clientes por status.
    /// </summary>
    Task<IEnumerable<Customer>> GetByStatusAsync(CustomerStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista apenas leads (status < Customer).
    /// </summary>
    Task<IEnumerable<Customer>> GetLeadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista apenas clientes ativos.
    /// </summary>
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca clientes por origem.
    /// </summary>
    Task<IEnumerable<Customer>> GetBySourceAsync(LeadSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um cliente com o e-mail informado.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca paginada com filtros e sorting.
    /// </summary>
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CustomerStatus? status = null,
        LeadSource? source = null,
        string? sortBy = null,
        bool sortDescending = false,
        bool? hasEmail = null,
        bool? hasWhatsApp = null,
        PersonType? personType = null,
        LeadSegment? segment = null,
        bool? onlyLeads = null,
        bool? onlyCustomers = null,
        CancellationToken cancellationToken = default);
}
