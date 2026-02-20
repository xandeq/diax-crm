using Diax.Domain.Common;

namespace Diax.Domain.EmailMarketing;

public interface IEmailQueueRepository : IRepository<EmailQueueItem>
{
    Task AddRangeAsync(IEnumerable<EmailQueueItem> items, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailQueueItem>> GetPendingBatchAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default);
    Task<int> CountSentSinceAsync(DateTime fromUtc, CancellationToken cancellationToken = default);
    Task<(IEnumerable<EmailQueueItem> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailQueueItem>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
