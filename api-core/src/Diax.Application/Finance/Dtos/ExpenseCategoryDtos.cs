namespace Diax.Application.Finance.Dtos;

public record ExpenseCategoryResponse(
    Guid Id,
    string Name,
    bool IsActive
);

public record CreateExpenseCategoryRequest(
    string Name
);

public record UpdateExpenseCategoryRequest(
    string Name,
    bool IsActive
);
