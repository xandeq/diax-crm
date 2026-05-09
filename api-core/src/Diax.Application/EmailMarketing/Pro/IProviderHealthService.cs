using Diax.Application.EmailMarketing.Pro.Dtos;

namespace Diax.Application.EmailMarketing.Pro;

public interface IProviderHealthService
{
    Task<List<ProviderHealthDto>> GetHealthAsync(CancellationToken cancellationToken = default);
}
