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
        Guid? financialAccountId = null)
    {
        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        Category = category;
        IsRecurring = isRecurring;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;
        FinancialAccountId = financialAccountId;
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
        Guid? financialAccountId = null)
    {
        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        Category = category;
        IsRecurring = isRecurring;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;
        FinancialAccountId = financialAccountId;
    }
}
