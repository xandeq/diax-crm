namespace Diax.Application.Finance.Dtos;

public record CreditCardResponse(
    Guid Id,
    string Name,
    string LastFourDigits,
    int ClosingDay,
    int DueDay,
    decimal Limit,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
