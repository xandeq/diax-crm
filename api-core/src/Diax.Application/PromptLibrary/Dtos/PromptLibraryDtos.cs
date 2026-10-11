namespace Diax.Application.PromptLibrary.Dtos;

/// <summary>Metadados de uma variável <c>{{nome}}</c>. Tipo: text | textarea | select | number.</summary>
public sealed record PromptVariableDto(
    string Name,
    string? Label = null,
    string Type = "text",
    string? Default = null,
    bool Required = true,
    IReadOnlyList<string>? Options = null);

public sealed record PromptListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    string Kind,
    string? Framework,
    Guid? CollectionId,
    IReadOnlyList<string> Tags,
    string Preview,
    IReadOnlyList<string> VariableNames,
    bool IsFavorite,
    bool IsArchived,
    bool ExposeToMcp,
    int CurrentVersionNumber,
    int UseCount,
    DateTime? LastUsedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Source);

public sealed record PromptVersionDto(
    int Number,
    string Content,
    IReadOnlyList<PromptVariableDto> Variables,
    string? ChangeNote,
    string Source,
    string? FrameworkApplied,
    DateTime CreatedAt);

public sealed record PromptVersionSummaryDto(
    int Number,
    string? ChangeNote,
    string Source,
    string? FrameworkApplied,
    int ContentLength,
    DateTime CreatedAt);

public sealed record PromptDetailDto(
    PromptListItemDto Prompt,
    string? TargetModel,
    PromptVersionDto Current);

public sealed class PromptListQuery
{
    public string? Q { get; set; }
    public string? Tag { get; set; }
    public Guid? CollectionId { get; set; }
    public bool? Favorite { get; set; }
    public string? Kind { get; set; }
    public bool Archived { get; set; }
    public bool? ExposeToMcp { get; set; }
    public string Sort { get; set; } = "updated";
    public int Limit { get; set; } = 2000;
}

public sealed class CreatePromptRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<PromptVariableDto>? Variables { get; set; }
    public List<string>? Tags { get; set; }
    public Guid? CollectionId { get; set; }
    public string? Kind { get; set; }
    public string? Framework { get; set; }
    public string? TargetModel { get; set; }
    public bool ExposeToMcp { get; set; }
    public bool IsFavorite { get; set; }
    public string? ChangeNote { get; set; }
    public string? Source { get; set; }
}

/// <summary>PATCH de metadados: campos nulos não são alterados. Não cria versão.</summary>
public sealed class UpdatePromptRequest
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public Guid? CollectionId { get; set; }
    public bool ClearCollection { get; set; }
    public string? Kind { get; set; }
    public string? Framework { get; set; }
    public string? TargetModel { get; set; }
    public bool? IsFavorite { get; set; }
    public bool? IsArchived { get; set; }
    public bool? ExposeToMcp { get; set; }
}

public sealed class AddPromptVersionRequest
{
    public string Content { get; set; } = string.Empty;
    public List<PromptVariableDto>? Variables { get; set; }
    public string? ChangeNote { get; set; }
    public int BaseVersionNumber { get; set; }
    public string? Source { get; set; }
    public string? FrameworkApplied { get; set; }
}

public sealed record AddPromptVersionResult(PromptVersionDto Version, bool Created);

public sealed class RenderPromptRequest
{
    public Dictionary<string, string?>? Variables { get; set; }
    public int? VersionNumber { get; set; }
    public bool Strict { get; set; }
}

public sealed record RenderPromptResult(
    string Rendered,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Unused,
    IReadOnlyList<PromptVariableDto> Variables,
    int VersionNumber);

public sealed record PromptTagCountDto(string Name, int Count);

public sealed class RenameTagRequest
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public sealed record PromptCollectionDto(Guid Id, string Name, string? Icon, string? Color, int SortOrder, int PromptCount);

public sealed class SavePromptCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public sealed class ReorderCollectionsRequest
{
    public List<Guid> Ids { get; set; } = new();
}

public sealed record UsePromptRequest(string? Channel);
