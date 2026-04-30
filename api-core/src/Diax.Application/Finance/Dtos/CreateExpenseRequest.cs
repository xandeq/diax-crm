using System.ComponentModel.DataAnnotations;
using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

/// <summary>
/// Request for creating an expense.
/// BUSINESS RULES:
/// - ExpenseCategoryId is REQUIRED
/// - For CreditCard payments: CreditCardId is REQUIRED, FinancialAccountId must be NULL
/// - For all other payments (Cash, DebitCard, Pix, BankTransfer, Boleto): FinancialAccountId is REQUIRED, CreditCardId must be NULL
/// </summary>
public record CreateExpenseRequest(
    [property: Required, StringLength(500, MinimumLength = 1)] string Description,
    [property: Range(typeof(decimal), "0.01", "999999999.99")] decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    Guid ExpenseCategoryId,
    bool IsRecurring,
    Guid? CreditCardId,
    Guid? CreditCardInvoiceId = null,
    Guid? FinancialAccountId = null,
    ExpenseStatus Status = ExpenseStatus.Pending,
    DateTime? PaidDate = null
);
