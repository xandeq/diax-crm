using Diax.Domain.Common;

namespace Diax.Domain.AI;

public interface IAiProviderRepository : IRepository<AiProvider>
{
    Task<AiProvider?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiProvider>> GetAllIncludedAsync(CancellationToken cancellationToken = default);
}
