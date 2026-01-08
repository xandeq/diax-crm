using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class IncomeCategory : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // Constructor for EF Core
    protected IncomeCategory() { }

    public IncomeCategory(string name, bool isActive = true)
    {
        Name = name;
        IsActive = isActive;
    }

    public void Update(string name, bool isActive)
    {
        Name = name;
        IsActive = isActive;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
