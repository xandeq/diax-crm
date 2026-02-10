using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Diax.Shared.Results;
using System.Linq.Expressions;

namespace Diax.Infrastructure.Data.Repositories;

public class ExpenseRepository : Repository<Expense>, IExpenseRepository
{
    public ExpenseRepository(DiaxDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.ExpenseCategory)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public override async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.ExpenseCategory)
            .Include(e => e.CreditCard)
            .Include(e => e.FinancialAccount)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.ExpenseCategory)
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Expense>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(e => e.ExpenseCategory)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<Expense?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(e => e.ExpenseCategory)
            .Include(e => e.CreditCard)
            .Include(e => e.FinancialAccount)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
    }
    public override async Task<PagedResult<Expense>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<Expense, bool>>? predicate = null,
        Func<IQueryable<Expense>, IOrderedQueryable<Expense>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Expense> query = DbSet
            .Include(e => e.ExpenseCategory)
            .Include(e => e.CreditCard)
            .Include(e => e.FinancialAccount);

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

        return new PagedResult<Expense>(items, totalCount, page, pageSize);
    }
}
