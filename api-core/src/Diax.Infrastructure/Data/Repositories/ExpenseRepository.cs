using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

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
            .Include(e => e.ExpenseCategory)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<Expense?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(e => e.ExpenseCategory)
            .Include(e => e.CreditCard)
            .Include(e => e.FinancialAccount)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
    }
}
