using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;

namespace Diax.Application.Finance.Planner.Dtos;

public class RecurringTransactionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public FrequencyType FrequencyType { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRecurringTransactionRequest
{
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public FrequencyType FrequencyType { get; set; } = FrequencyType.Monthly;
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public int Priority { get; set; } = 50;
}
