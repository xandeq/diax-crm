using Diax.Domain.Household;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ChecklistItemRepository : Repository<ChecklistItem>, IChecklistItemRepository
{
    public ChecklistItemRepository(DiaxDbContext context) : base(context)
    {
    }

    public override async Task<ChecklistItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }
}
