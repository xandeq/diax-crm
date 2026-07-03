using Diax.Domain.Common;

namespace Diax.Domain.Tasks;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetByUserAsync(Guid userId, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetByStatusAsync(Guid userId, TaskItemStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetOverdueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// IDs de customers (dentre os informados) que JÁ têm task aberta
    /// (Todo/InProgress, não arquivada) — dedup dos follow-ups automáticos.
    /// </summary>
    Task<HashSet<Guid>> GetCustomerIdsWithOpenTasksAsync(
        IEnumerable<Guid> customerIds, CancellationToken cancellationToken = default);
}
