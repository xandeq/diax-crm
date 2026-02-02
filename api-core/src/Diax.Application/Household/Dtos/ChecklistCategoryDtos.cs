namespace Diax.Application.Household.Dtos;

public record ChecklistCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateChecklistCategoryRequest(
    string Name,
    string? Color = null,
    int SortOrder = 0);

public record UpdateChecklistCategoryRequest(
    string Name,
    string? Color = null,
    int SortOrder = 0);
