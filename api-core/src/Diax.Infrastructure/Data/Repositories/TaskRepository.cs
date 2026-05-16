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
        var query = _context.TaskItems.Where(t => t.UserId == userId);
        if (!includeArchived)
            query = query.Where(t => !t.IsArchived);
        return await query.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(Guid userId, TaskItemStatus status, CancellationToken cancellationToken = default)
        => await _context.TaskItems
            .Where(t => t.UserId == userId && t.Status == status && !t.IsArchived)
            .OrderByDescending(t => t.Priority)
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
}
