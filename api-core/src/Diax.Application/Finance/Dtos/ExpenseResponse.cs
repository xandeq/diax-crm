using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record ExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    Guid ExpenseCategoryId,
    string? ExpenseCategoryName,
    bool IsRecurring,
    Guid? CreditCardId,
    string? CreditCardName, // New
    Guid? CreditCardInvoiceId,
    Guid? FinancialAccountId,
    string? FinancialAccountName, // New
    ExpenseStatus Status,
    DateTime? PaidDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
