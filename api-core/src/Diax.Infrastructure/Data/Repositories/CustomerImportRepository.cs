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
    /// Obtém lista paginada de importações ordenadas por data de criação (mais recentes primeiro),
    /// opcionalmente restrita a um período (EXTR-02).
    /// </summary>
    public async Task<(IEnumerable<CustomerImport> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CustomerImport> query = DbSet;

        // Filtro por período — usa o índice IX_CustomerImports_CreatedAt que já existe.
        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
