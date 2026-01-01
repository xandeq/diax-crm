namespace Diax.Application.Finance.Dtos;

public record UpdateCreditCardRequest(
    string Name,
    string LastFourDigits,
    int ClosingDay,
    int DueDay,
    decimal Limit
);
