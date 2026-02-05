using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ExpenseCategoryRepository : Repository<ExpenseCategory>, IExpenseCategoryRepository
{
    public ExpenseCategoryRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ExpenseCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExpenseCategory>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<ExpenseCategory?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
