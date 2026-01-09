namespace Diax.Application.Finance.Dtos;

public record CreditCardInvoiceResponse(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    int ReferenceMonth,
    int ReferenceYear,
    DateTime ClosingDate,
    DateTime DueDate,
    decimal TotalAmount,
    bool IsPaid,
    DateTime? PaymentDate,
    Guid? PaidFromAccountId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
