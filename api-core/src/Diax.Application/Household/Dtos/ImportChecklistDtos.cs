namespace Diax.Application.Household.Dtos;

public record ImportChecklistRequest(
    List<ImportChecklistItemDto> Items
);

public record ImportChecklistItemDto(
    string Title,
    string Category,
    string? Description = null,
    decimal? EstimatedPrice = null,
    int Quantity = 1,
    string? Priority = "Medium"
);
