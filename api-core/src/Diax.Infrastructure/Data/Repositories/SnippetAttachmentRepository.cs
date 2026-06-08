using Diax.Domain.Snippets;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class SnippetAttachmentRepository : Repository<SnippetAttachment>, ISnippetAttachmentRepository
{
    public SnippetAttachmentRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<List<SnippetAttachment>> GetBySnippetIdAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.SnippetId == snippetId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
