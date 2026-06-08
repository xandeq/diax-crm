using Diax.Domain.Common;

namespace Diax.Domain.Snippets;

public interface ISnippetAttachmentRepository : IRepository<SnippetAttachment>
{
    Task<List<SnippetAttachment>> GetBySnippetIdAsync(Guid snippetId, CancellationToken cancellationToken = default);
}
