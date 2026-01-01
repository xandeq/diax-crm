using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record IncomeResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    string? Category,
    bool IsRecurring,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
