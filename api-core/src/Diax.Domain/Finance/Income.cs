using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class Income : AuditableEntity
{
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? Category { get; private set; }
    public bool IsRecurring { get; private set; }

    public Income(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        string? category,
        bool isRecurring)
    {
        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        Category = category;
        IsRecurring = isRecurring;
    }

    public void Update(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        string? category,
        bool isRecurring)
    {
        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        Category = category;
        IsRecurring = isRecurring;
    }
}
