using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UpdateCreditCardRequest(
    string Name,
    string LastFourDigits,
    int ClosingDay,
    int DueDay,
    decimal Limit,
    CardBrand Brand,
    CardKind CardKind,
    bool IsActive,
    Guid? CreditCardGroupId
);
