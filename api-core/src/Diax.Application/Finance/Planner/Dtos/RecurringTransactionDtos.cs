using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;

namespace Diax.Application.Finance.Planner.Dtos;

// Alias to avoid ambiguity with Diax.Domain.Finance.TransactionType (unified)
using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

public class RecurringTransactionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PlannerTransactionType Type { get; set; }
    public RecurringItemKind ItemKind { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
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
    public bool IsSubscription { get; set; }
    public bool HasVariableAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRecurringTransactionRequest
{
    public PlannerTransactionType Type { get; set; }
    public RecurringItemKind ItemKind { get; set; } = RecurringItemKind.Standard;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
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
    public bool HasVariableAmount { get; set; }
}

public class UpdateRecurringTransactionRequest
{
    public PlannerTransactionType Type { get; set; }
    public RecurringItemKind ItemKind { get; set; } = RecurringItemKind.Standard;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public FrequencyType FrequencyType { get; set; } = FrequencyType.Monthly;
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 50;
    public bool HasVariableAmount { get; set; }
}
