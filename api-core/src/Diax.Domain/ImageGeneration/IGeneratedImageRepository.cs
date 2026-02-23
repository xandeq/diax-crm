using Diax.Domain.Common;

namespace Diax.Domain.ImageGeneration;

public interface IGeneratedImageRepository : IRepository<GeneratedImage>
{
    Task<List<GeneratedImage>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<List<GeneratedImage>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
}
