namespace Diax.Domain.Finance.Assets;

public interface INextActionRepository
{
    Task<List<NextAction>> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<NextAction?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<NextAction> actions, CancellationToken cancellationToken = default);
    Task UpdateAsync(NextAction action, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<NextAction> actions, CancellationToken cancellationToken = default);
}
