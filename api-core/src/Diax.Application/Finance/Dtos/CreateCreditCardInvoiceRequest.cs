namespace Diax.Application.Finance.Dtos;

public record CreateCreditCardInvoiceRequest(
    Guid CreditCardId,
    int ReferenceMonth,
    int ReferenceYear
);
