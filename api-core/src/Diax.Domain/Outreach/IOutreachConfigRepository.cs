using Diax.Domain.Common;

namespace Diax.Domain.Outreach;

public interface IOutreachConfigRepository : IRepository<OutreachConfig>
{
    Task<OutreachConfig?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
