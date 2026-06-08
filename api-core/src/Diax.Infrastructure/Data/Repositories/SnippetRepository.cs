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

    public async Task<IEnumerable<Snippet>> GetByUserIdWithAttachmentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Attachments)
            .Where(s => s.CreatedByUserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Snippet?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
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

    public async Task<Snippet?> GetPublicByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsPublic, cancellationToken);
    }
}
