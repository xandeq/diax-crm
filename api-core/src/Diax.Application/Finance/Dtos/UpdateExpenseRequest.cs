using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UpdateExpenseRequest(
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    string? Category,
    bool IsRecurring,
    Guid? CreditCardId,
    Guid? CreditCardInvoiceId = null,
    Guid? FinancialAccountId = null
);
