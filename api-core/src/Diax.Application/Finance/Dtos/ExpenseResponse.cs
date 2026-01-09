using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record ExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    string? Category,
    bool IsRecurring,
    Guid? CreditCardId,
    Guid? CreditCardInvoiceId,
    Guid? FinancialAccountId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
