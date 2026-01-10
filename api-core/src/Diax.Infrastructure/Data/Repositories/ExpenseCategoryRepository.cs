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
}
