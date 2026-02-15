using Diax.Domain.Common;

namespace Diax.Domain.Customers;

/// <summary>
/// Repositório para operações com CustomerImport.
/// </summary>
public interface ICustomerImportRepository : IRepository<CustomerImport>
{
    /// <summary>
    /// Obtém lista paginada de importações.
    /// </summary>
    /// <param name="page">Número da página (1-based)</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de importações e total de registros</returns>
    Task<(IEnumerable<CustomerImport> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
