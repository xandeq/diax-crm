using Diax.Domain.Common;

namespace Diax.Domain.ImageGeneration;

public interface IImageGenerationProjectRepository : IRepository<ImageGenerationProject>
{
    Task<ImageGenerationProject?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ImageGenerationProject>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
