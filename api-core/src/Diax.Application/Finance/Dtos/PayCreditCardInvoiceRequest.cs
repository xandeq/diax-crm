namespace Diax.Application.Finance.Dtos;

public record PayCreditCardInvoiceRequest(
    DateTime PaymentDate,
    Guid? PaidFromAccountId = null,
    decimal? StatementAmount = null
);
