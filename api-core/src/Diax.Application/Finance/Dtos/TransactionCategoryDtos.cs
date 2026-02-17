using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record TransactionCategoryResponse(
    Guid Id,
    string Name,
    bool IsActive,
    CategoryApplicableTo ApplicableTo,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateTransactionCategoryRequest(
    string Name,
    CategoryApplicableTo ApplicableTo,
    bool IsActive = true
);

public record UpdateTransactionCategoryRequest(
    string Name,
    CategoryApplicableTo ApplicableTo,
    bool IsActive
);
