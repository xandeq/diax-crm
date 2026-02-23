using Diax.Domain.Common;

namespace Diax.Domain.ImageGeneration;

public interface IImageTemplateRepository : IRepository<ImageTemplate>
{
    Task<List<ImageTemplate>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<List<ImageTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
