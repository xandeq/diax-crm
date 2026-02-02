using Diax.Domain.Common;

namespace Diax.Domain.Household;

public class ChecklistItem : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChecklistItemStatus Status { get; set; } = ChecklistItemStatus.ToBuy;
    public ChecklistItemPriority? Priority { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? BoughtAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? ActualPrice { get; set; }
    public decimal? Quantity { get; set; }
    public string? StoreOrLink { get; set; }
    public bool IsArchived { get; set; }

    public virtual ChecklistCategory Category { get; set; } = null!;
}
