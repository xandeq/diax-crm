using Diax.Domain.Common;

namespace Diax.Domain.Tasks;

public class TaskItem : AuditableEntity, IUserOwnedEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsArchived { get; set; } = false;

    // IUserOwnedEntity
    public Guid UserId { get; set; }

    public void Complete()
    {
        Status = TaskItemStatus.Done;
        CompletedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = TaskItemStatus.Todo;
        CompletedAt = null;
    }

    public void Cancel()
    {
        Status = TaskItemStatus.Cancelled;
    }

    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;
}
