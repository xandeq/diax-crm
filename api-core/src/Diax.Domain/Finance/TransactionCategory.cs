using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Categoria unificada de transação — substitui IncomeCategory e ExpenseCategory.
/// O campo ApplicableTo define se a categoria pode ser usada em receitas, despesas ou ambos.
/// </summary>
public class TransactionCategory : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    /// <summary>
    /// Define a qual tipo de transação esta categoria se aplica
    /// </summary>
    public CategoryApplicableTo ApplicableTo { get; private set; }

    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    // EF Core constructor
    protected TransactionCategory() { }

    public TransactionCategory(string name, Guid userId, CategoryApplicableTo applicableTo, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da categoria não pode ser vazio.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Nome da categoria não pode exceder 200 caracteres.", nameof(name));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        Name = name;
        UserId = userId;
        ApplicableTo = applicableTo;
        IsActive = isActive;
    }

    public void Update(string name, bool isActive, CategoryApplicableTo applicableTo)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da categoria não pode ser vazio.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Nome da categoria não pode exceder 200 caracteres.", nameof(name));

        Name = name;
        IsActive = isActive;
        ApplicableTo = applicableTo;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
