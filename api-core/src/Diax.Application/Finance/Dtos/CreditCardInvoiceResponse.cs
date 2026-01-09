namespace Diax.Application.Finance.Dtos;

public record CreditCardInvoiceResponse(
    Guid Id,
    Guid CreditCardGroupId,
    string CreditCardGroupName,
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
