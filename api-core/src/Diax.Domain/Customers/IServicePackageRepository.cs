using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public interface IServicePackageRepository : IRepository<ServicePackage>
{
    Task<List<ServicePackage>> GetByUserAsync(Guid userId, bool onlyActive, CancellationToken ct = default);
}
