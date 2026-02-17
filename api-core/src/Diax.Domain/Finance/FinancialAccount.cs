using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Representa uma conta financeira onde receitas entram e despesas podem sair.
/// Cartão de crédito NÃO é uma conta financeira.
/// </summary>
public class FinancialAccount : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public decimal InitialBalance { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    // Legacy navigation properties (mantidas para período de migração)
    [Obsolete("Use Transactions instead")]
    public virtual ICollection<Income> Incomes { get; private set; } = new List<Income>();
    [Obsolete("Use Transactions instead")]
    public virtual ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    protected FinancialAccount() { }

    public FinancialAccount(
        string name,
        AccountType accountType,
        decimal initialBalance,
        Guid userId,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Account name cannot exceed 200 characters", nameof(name));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        Name = name;
        AccountType = accountType;
        InitialBalance = initialBalance;
        Balance = initialBalance;
        UserId = userId;
        IsActive = isActive;
    }

    public void Update(
        string name,
        AccountType accountType,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Account name cannot exceed 200 characters", nameof(name));

        Name = name;
        AccountType = accountType;
        IsActive = isActive;
    }

    public void UpdateBalance(decimal newBalance)
    {
        Balance = newBalance;
    }

    /// <summary>
    /// Credita valor na conta (aumenta saldo)
    /// </summary>
    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be greater than zero", nameof(amount));

        Balance += amount;
    }

    /// <summary>
    /// Debita valor da conta (diminui saldo)
    /// </summary>
    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Debit amount must be greater than zero", nameof(amount));

        Balance -= amount;
    }

    [Obsolete("Use Credit() instead")]
    public void AddToBalance(decimal amount)
    {
        Balance += amount;
    }

    [Obsolete("Use Debit() instead")]
    public void SubtractFromBalance(decimal amount)
    {
        Balance -= amount;
    }
}
