using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;

namespace Diax.Application.Finance.Dtos;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

public class PersonalFinanceMonthRequest
{
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class PersonalFinanceMonthResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public IReadOnlyList<TransactionResponse> Items { get; set; } = Array.Empty<TransactionResponse>();
    public IReadOnlyList<TransactionResponse> Incomes { get; set; } = Array.Empty<TransactionResponse>();
    public IReadOnlyList<TransactionResponse> Expenses { get; set; } = Array.Empty<TransactionResponse>();
    public IReadOnlyList<RecurringOccurrenceResponse> RecurringItems { get; set; } = Array.Empty<RecurringOccurrenceResponse>();
    public IReadOnlyList<RecurringOccurrenceResponse> Subscriptions { get; set; } = Array.Empty<RecurringOccurrenceResponse>();
    public IReadOnlyList<CreditCardMonthlySummaryResponse> CreditCards { get; set; } = Array.Empty<CreditCardMonthlySummaryResponse>();
    public IReadOnlyList<CreditCardInvoiceMonthlySummaryResponse> CreditCardInvoices { get; set; } = Array.Empty<CreditCardInvoiceMonthlySummaryResponse>();
    public PersonalFinanceMonthSummaryResponse Summary { get; set; } = new();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

public class PersonalFinanceMonthSummaryResponse
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalCreditExpenses { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal PaidExpenses { get; set; }
    public decimal UnpaidExpenses { get; set; }
    public decimal ExpensesWithCard { get; set; }
    public decimal ExpensesWithoutCard { get; set; }
    public decimal ProjectedIncome { get; set; }
    public decimal ProjectedExpenses { get; set; }
    public decimal ProjectedRemainingBalance { get; set; }
    public decimal SubscriptionTotal { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
}

public class RecurringOccurrenceResponse
{
    public Guid SourceRecurringTransactionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public PlannerTransactionType Type { get; set; }
    public RecurringItemKind ItemKind { get; set; }
    public FrequencyType FrequencyType { get; set; }
    public int DayOfMonth { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public bool IsSubscription { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class CreditCardMonthlySummaryResponse
{
    public Guid CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public Guid? CreditCardGroupId { get; set; }
    public string? CreditCardGroupName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int ExpenseCount { get; set; }
    public bool HasInvoice { get; set; }
    public decimal InvoiceAmount { get; set; }
    public bool InvoicePaid { get; set; }
    public DateTime? InvoicePaymentDate { get; set; }
    public DateTime? InvoiceDueDate { get; set; }
    public decimal? StatementAmount { get; set; }
    public Guid? InvoiceId { get; set; }
}

public class CreditCardInvoiceMonthlySummaryResponse
{
    public Guid Id { get; set; }
    public Guid CreditCardGroupId { get; set; }
    public string CreditCardGroupName { get; set; } = string.Empty;
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public DateTime ClosingDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public Guid? PaidFromAccountId { get; set; }
    public string? PaidFromAccountName { get; set; }
}
