using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Representa uma receita que SEMPRE entra em uma conta financeira
/// </summary>
public class Income : AuditableEntity
{
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Guid IncomeCategoryId { get; private set; }
    public virtual IncomeCategory? IncomeCategory { get; private set; }
    public bool IsRecurring { get; private set; }

    public Guid FinancialAccountId { get; private set; }
    public virtual FinancialAccount? FinancialAccount { get; private set; }

    protected Income() { }

    public Income(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid incomeCategoryId,
        bool isRecurring,
        Guid financialAccountId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (financialAccountId == Guid.Empty)
            throw new ArgumentException("Income must be linked to a financial account", nameof(financialAccountId));

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        IncomeCategoryId = incomeCategoryId;
        IsRecurring = isRecurring;
        FinancialAccountId = financialAccountId;
    }

    public void Update(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid incomeCategoryId,
        bool isRecurring,
        Guid financialAccountId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (financialAccountId == Guid.Empty)
            throw new ArgumentException("Income must be linked to a financial account", nameof(financialAccountId));

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        IncomeCategoryId = incomeCategoryId;
        IsRecurring = isRecurring;
        FinancialAccountId = financialAccountId;
    }
}
