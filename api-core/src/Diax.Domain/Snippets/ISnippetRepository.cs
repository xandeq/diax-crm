using Diax.Domain.Common;

namespace Diax.Domain.Snippets;

public interface ISnippetRepository : IRepository<Snippet>
{
    Task<IEnumerable<Snippet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Snippet>> GetByUserIdWithAttachmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Snippet?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Snippet?> GetByIdWithUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Snippet?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Snippet?> GetPublicByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken = default);
}
