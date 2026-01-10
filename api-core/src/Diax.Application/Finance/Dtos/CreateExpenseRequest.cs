using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

/// <summary>
/// Request for creating an expense.
/// BUSINESS RULES:
/// - For CreditCard payments: CreditCardId is REQUIRED, FinancialAccountId must be NULL
/// - For all other payments (Cash, DebitCard, Pix, BankTransfer, Boleto): FinancialAccountId is REQUIRED, CreditCardId must be NULL
/// </summary>
public record CreateExpenseRequest(
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    string? Category,
    bool IsRecurring,
    Guid? CreditCardId,
    Guid? CreditCardInvoiceId = null,
    Guid? FinancialAccountId = null,
    ExpenseStatus Status = ExpenseStatus.Pending,
    DateTime? PaidDate = null
);
