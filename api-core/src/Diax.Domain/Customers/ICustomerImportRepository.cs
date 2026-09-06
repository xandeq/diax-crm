using Diax.Domain.Common;

namespace Diax.Domain.Customers;

/// <summary>
/// Repositório para operações com CustomerImport.
/// </summary>
public interface ICustomerImportRepository : IRepository<CustomerImport>
{
    /// <summary>
    /// Obtém lista paginada de importações, opcionalmente restrita a um período (EXTR-02:
    /// "motivo de rejeição consultável por período"). Datas são UTC e inclusivas nas bordas.
    /// </summary>
    Task<(IEnumerable<CustomerImport> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
