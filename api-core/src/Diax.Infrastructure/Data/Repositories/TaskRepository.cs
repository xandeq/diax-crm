using Diax.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class TaskRepository : Repository<TaskItem>, ITaskRepository
{
    private readonly DiaxDbContext _context;

    public TaskRepository(DiaxDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task<IEnumerable<TaskItem>> GetByUserAsync(Guid userId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _context.TaskItems.IgnoreQueryFilters().Where(t => t.UserId == userId);
        if (!includeArchived)
            query = query.Where(t => !t.IsArchived);
        return await query.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(Guid userId, TaskItemStatus status, CancellationToken cancellationToken = default)
        => await _context.TaskItems
            .Where(t => t.UserId == userId && t.Status == status && !t.IsArchived)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate == null) // SQL Server põe NULL primeiro: sem prazo vai para o fim
            .ThenBy(t => t.DueDate == null) // SQL Server põe NULL primeiro: sem prazo vai para o fim
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TaskItem>> GetOverdueAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.TaskItems
            .Where(t => t.UserId == userId
                && t.DueDate < DateTime.UtcNow
                && t.Status != TaskItemStatus.Done
                && t.Status != TaskItemStatus.Cancelled
                && !t.IsArchived)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, NextTaskInfo>> GetNextOpenTasksByCustomerAsync(
        IEnumerable<Guid> customerIds, CancellationToken cancellationToken = default)
    {
        var ids = customerIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, NextTaskInfo>();

        var rows = await _context.TaskItems
            .Where(t => t.CustomerId != null
                && ids.Contains(t.CustomerId.Value)
                && (t.Status == TaskItemStatus.Todo || t.Status == TaskItemStatus.InProgress)
                && !t.IsArchived)
            .Select(t => new { CustomerId = t.CustomerId!.Value, t.Title, t.DueDate, t.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.CustomerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var next = g.OrderBy(r => r.DueDate ?? DateTime.MaxValue).ThenBy(r => r.CreatedAt).First();
                    return new NextTaskInfo(next.Title, next.DueDate);
                });
    }

    public async Task<HashSet<Guid>> GetCustomerIdsWithOpenTasksAsync(
        IEnumerable<Guid> customerIds, CancellationToken cancellationToken = default)
    {
        var ids = customerIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<Guid>();

        var found = await _context.TaskItems
            .IgnoreQueryFilters() // chamado pelo LeadScoringWorker (sem usuário corrente)
            .Where(t => t.CustomerId != null
                && ids.Contains(t.CustomerId.Value)
                && (t.Status == TaskItemStatus.Todo || t.Status == TaskItemStatus.InProgress)
                && !t.IsArchived)
            .Select(t => t.CustomerId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return found.ToHashSet();
    }
}
