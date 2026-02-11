using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Diax.Shared.Results;
using System.Linq.Expressions;

namespace Diax.Infrastructure.Data.Repositories;

public class IncomeRepository : Repository<Income>, IIncomeRepository
{
    public IncomeRepository(DiaxDbContext context) : base(context)
    {
    }

    public override async Task<Income?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Include(i => i.FinancialAccount)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<Income>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Include(i => i.FinancialAccount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Income>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Include(i => i.FinancialAccount)
            .Where(i => i.Date.Year == year && i.Date.Month == month)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Income>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Include(i => i.FinancialAccount)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<Income?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Include(i => i.FinancialAccount)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }

    public override async Task<PagedResult<Income>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<Income, bool>>? predicate = null,
        Func<IQueryable<Income>, IOrderedQueryable<Income>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Income> query = DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Include(i => i.FinancialAccount);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Income>(items, totalCount, page, pageSize);
    }
}
