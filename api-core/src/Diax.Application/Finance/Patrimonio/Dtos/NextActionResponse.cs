using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

public record NextActionResponse(
    Guid Id,
    string Title,
    string Rationale,
    string Category,
    decimal? SuggestedAmount,
    AssetClass? TargetClass,
    string Status,
    Guid? LinkedTaskId,
    DateTime CreatedAt
);
