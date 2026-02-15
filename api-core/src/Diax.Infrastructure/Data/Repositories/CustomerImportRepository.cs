using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório de CustomerImport.
/// </summary>
public class CustomerImportRepository : Repository<CustomerImport>, ICustomerImportRepository
{
    public CustomerImportRepository(DiaxDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Obtém lista paginada de importações ordenadas por data de criação (mais recentes primeiro).
    /// </summary>
    public async Task<(IEnumerable<CustomerImport> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
