using Diax.Shared.Results;

namespace Diax.Application.Customers;

public interface IApifyIntegrationService
{
    Task<Result<Guid>> ImportDatasetAsync(string datasetUrl, int source, CancellationToken cancellationToken = default);
}
