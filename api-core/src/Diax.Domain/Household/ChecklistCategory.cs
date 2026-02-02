using Diax.Domain.Common;

namespace Diax.Domain.Household;

public class ChecklistCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int SortOrder { get; set; }

    public virtual ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}
