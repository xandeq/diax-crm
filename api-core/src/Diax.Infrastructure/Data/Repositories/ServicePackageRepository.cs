using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ServicePackageRepository : Repository<ServicePackage>, IServicePackageRepository
{
    public ServicePackageRepository(DiaxDbContext context) : base(context) { }

    public async Task<List<ServicePackage>> GetByUserAsync(Guid userId, bool onlyActive, CancellationToken ct = default)
        => await DbSet.Where(p => p.UserId == userId && (!onlyActive || p.IsActive))
                      .OrderBy(p => p.SortOrder).ThenBy(p => p.Price)
                      .ToListAsync(ct);
}
