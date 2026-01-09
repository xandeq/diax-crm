using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Representa uma conta financeira onde receitas entram e despesas podem sair
/// </summary>
public class FinancialAccount : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public decimal InitialBalance { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public virtual ICollection<Income> Incomes { get; private set; } = new List<Income>();
    public virtual ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    protected FinancialAccount() { }

    public FinancialAccount(
        string name,
        AccountType accountType,
        decimal initialBalance,
        bool isActive = true)
    {
        Name = name;
        AccountType = accountType;
        InitialBalance = initialBalance;
        Balance = initialBalance;
        IsActive = isActive;
    }

    public void Update(
        string name,
        AccountType accountType,
        bool isActive)
    {
        Name = name;
        AccountType = accountType;
        IsActive = isActive;
    }

    public void UpdateBalance(decimal newBalance)
    {
        Balance = newBalance;
    }

    public void AddToBalance(decimal amount)
    {
        Balance += amount;
    }

    public void SubtractFromBalance(decimal amount)
    {
        Balance -= amount;
    }
}
