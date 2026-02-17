using Diax.Domain.Finance;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Diax.Infrastructure.Data.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(DiaxDbContext context) : base(context)
    {
    }

    public override async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Include(t => t.CreditCardInvoice)
            .Include(t => t.AccountTransfer)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);
    }

    public async Task<Transaction?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Include(t => t.CreditCardInvoice)
            .Include(t => t.AccountTransfer)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
    }

    public async Task<IEnumerable<Transaction>> GetByTypeAsync(TransactionType type, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId && t.Type == type)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Transaction>> GetByMonthAsync(int year, int month, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId && t.Date.Year == year && t.Date.Month == month)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid financialAccountId, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId && t.FinancialAccountId == financialAccountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Transaction>> GetByTransferGroupIdAsync(Guid transferGroupId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Where(t => t.TransferGroupId == transferGroupId)
            .ToListAsync(ct);
    }

    public override async Task<PagedResult<Transaction>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<Transaction, bool>>? predicate = null,
        Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> query = DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.FinancialAccount)
            .Include(t => t.CreditCard);

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

        return new PagedResult<Transaction>(items, totalCount, page, pageSize);
    }
}
