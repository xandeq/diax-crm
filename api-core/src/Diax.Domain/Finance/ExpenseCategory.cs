using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class ExpenseCategory : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // Constructor for EF Core
    protected ExpenseCategory() { }

    public ExpenseCategory(string name, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da categoria não pode ser vazio.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Nome da categoria não pode exceder 200 caracteres.", nameof(name));

        Name = name;
        IsActive = isActive;
    }

    public void Update(string name, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da categoria não pode ser vazio.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Nome da categoria não pode exceder 200 caracteres.", nameof(name));

        Name = name;
        IsActive = isActive;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
