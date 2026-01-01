namespace Diax.Application.Finance.Dtos;

public record CreateCreditCardRequest(
    string Name,
    string LastFourDigits,
    int ClosingDay,
    int DueDay,
    decimal Limit
);
