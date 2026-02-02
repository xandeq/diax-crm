using Diax.Domain.Household;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ChecklistCategoryRepository : Repository<ChecklistCategory>, IChecklistCategoryRepository
{
    public ChecklistCategoryRepository(DiaxDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<ChecklistCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
