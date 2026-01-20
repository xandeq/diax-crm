using Diax.Domain.Snippets;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class SnippetRepository : Repository<Snippet>, ISnippetRepository
{
    public SnippetRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Snippet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.CreatedByUserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Snippet?> GetByIdWithUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId, cancellationToken);
    }

    public async Task<Snippet?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.Id == id && s.IsPublic, cancellationToken);
    }
}
