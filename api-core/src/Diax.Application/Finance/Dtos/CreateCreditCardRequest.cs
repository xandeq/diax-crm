using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record CreateCreditCardRequest(
    string Name,
    string LastFourDigits,
    int ClosingDay,
    int DueDay,
    decimal Limit,
    CardBrand Brand = CardBrand.Unknown,
    CardKind CardKind = CardKind.Physical,
    bool IsActive = true,
    Guid? CreditCardGroupId = null
);
