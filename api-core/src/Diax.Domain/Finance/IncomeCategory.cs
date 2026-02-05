using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class IncomeCategory : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // Constructor for EF Core
    protected IncomeCategory() { }

    public IncomeCategory(string name, Guid userId, bool isActive = true)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        Name = name;
        UserId = userId;
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
