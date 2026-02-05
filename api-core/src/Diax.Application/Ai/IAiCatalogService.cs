using Diax.Application.AI.Dtos;

namespace Diax.Application.AI;

public interface IAiCatalogService
{
    Task<List<AiProviderDto>> GetUserCatalogAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateUserAccessAsync(Guid userId, string providerKey, string modelKey, CancellationToken cancellationToken = default);
}
