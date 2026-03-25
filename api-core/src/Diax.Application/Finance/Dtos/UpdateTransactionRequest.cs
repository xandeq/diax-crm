using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UpdateTransactionRequest(
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    Guid? CategoryId,
    bool IsRecurring,
    Guid? FinancialAccountId = null,
    Guid? CreditCardId = null,
    Guid? CreditCardInvoiceId = null,
    TransactionStatus? Status = null,
    DateTime? PaidDate = null,
    string? Details = null,
    bool? IsSubscription = null
);
