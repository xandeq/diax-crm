namespace Diax.Application.Finance.Dtos;

public record CreateCreditCardInvoiceRequest(
    Guid? CreditCardId,  // For backward compatibility - will lookup the group
    Guid? CreditCardGroupId,  // Preferred - direct group reference
    int ReferenceMonth,
    int ReferenceYear
);
