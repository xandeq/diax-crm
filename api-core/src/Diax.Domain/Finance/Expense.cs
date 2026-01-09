using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class Expense : AuditableEntity
{
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? Category { get; private set; }
    public bool IsRecurring { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public DateTime? PaidDate { get; private set; }

    public Guid? CreditCardId { get; private set; }
    public CreditCard? CreditCard { get; private set; }

    public Guid? CreditCardInvoiceId { get; private set; }
    public CreditCardInvoice? CreditCardInvoice { get; private set; }

    public Guid? FinancialAccountId { get; private set; }
    public FinancialAccount? FinancialAccount { get; private set; }

    public Expense(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        string? category,
        bool isRecurring,
        Guid? creditCardId = null,
        Guid? creditCardInvoiceId = null,
        Guid? financialAccountId = null,
        ExpenseStatus status = ExpenseStatus.Pending,
        DateTime? paidDate = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        Category = category;
        IsRecurring = isRecurring;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;
        FinancialAccountId = financialAccountId;
        Status = status;
        PaidDate = paidDate;
    }

    public void Update(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        string? category,
        bool isRecurring,
        Guid? creditCardId,
        Guid? creditCardInvoiceId = null,
        Guid? financialAccountId = null,
        ExpenseStatus status = ExpenseStatus.Pending,
        DateTime? paidDate = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        Category = category;
        IsRecurring = isRecurring;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;
        FinancialAccountId = financialAccountId;
        Status = status;
        PaidDate = paidDate;
    }

    public void MarkAsPaid(DateTime? paidDate = null)
    {
        Status = ExpenseStatus.Paid;
        PaidDate = paidDate ?? DateTime.UtcNow;
    }

    public void MarkAsPending()
    {
        Status = ExpenseStatus.Pending;
        PaidDate = null;
    }
}
