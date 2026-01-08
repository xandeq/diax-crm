namespace Diax.Application.Finance.Dtos;

public record IncomeCategoryResponse(
    Guid Id,
    string Name,
    bool IsActive
);

public record CreateIncomeCategoryRequest(
    string Name
);

public record UpdateIncomeCategoryRequest(
    string Name,
    bool IsActive
);
