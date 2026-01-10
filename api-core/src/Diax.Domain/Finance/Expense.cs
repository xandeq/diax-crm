using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class Expense : AuditableEntity
{
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    [Obsolete("Use ExpenseCategoryId instead")]
    public string? Category { get; private set; }
    public Guid ExpenseCategoryId { get; private set; }
    public ExpenseCategory? ExpenseCategory { get; private set; }
    public bool IsRecurring { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public DateTime? PaidDate { get; private set; }

    public Guid? CreditCardId { get; private set; }
    public CreditCard? CreditCard { get; private set; }

    public Guid? CreditCardInvoiceId { get; private set; }
    public CreditCardInvoice? CreditCardInvoice { get; private set; }

    public Guid? FinancialAccountId { get; private set; }
    public FinancialAccount? FinancialAccount { get; private set; }

    // Constructor for EF Core
    protected Expense() { }

    public Expense(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid expenseCategoryId,
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

        if (expenseCategoryId == Guid.Empty)
            throw new ArgumentException("ExpenseCategoryId cannot be empty", nameof(expenseCategoryId));

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        ExpenseCategoryId = expenseCategoryId;
        IsRecurring = isRecurring;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;
        FinancialAccountId = financialAccountId;
        Status = status;
        PaidDate = paidDate;

        ValidatePaymentMethodConstraints();
    }

    private void ValidatePaymentMethodConstraints()
    {
        if (PaymentMethod == PaymentMethod.CreditCard)
        {
            // Despesa no cartão: DEVE ter CreditCardId, NÃO PODE ter FinancialAccountId
            if (CreditCardId == null || CreditCardId == Guid.Empty)
                throw new ArgumentException("Credit card expenses must have a valid CreditCardId", nameof(CreditCardId));

            if (FinancialAccountId != null)
                throw new ArgumentException("Credit card expenses cannot be linked to a FinancialAccount (impact is on invoice payment)", nameof(FinancialAccountId));
        }
        else
        {
            // Despesa à vista: DEVE ter FinancialAccountId, NÃO PODE ter CreditCardId
            if (FinancialAccountId == null || FinancialAccountId == Guid.Empty)
                throw new ArgumentException($"Cash expenses ({PaymentMethod}) must have a valid FinancialAccountId", nameof(FinancialAccountId));

            if (CreditCardId != null)
                throw new ArgumentException($"Cash expenses ({PaymentMethod}) cannot be linked to a credit card", nameof(CreditCardId));
        }
    }

    public void Update(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid expenseCategoryId,
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

        if (expenseCategoryId == Guid.Empty)
            throw new ArgumentException("ExpenseCategoryId cannot be empty", nameof(expenseCategoryId));

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        ExpenseCategoryId = expenseCategoryId;
        IsRecurring = isRecurring;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;
        FinancialAccountId = financialAccountId;
        Status = status;
        PaidDate = paidDate;

        ValidatePaymentMethodConstraints();
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
