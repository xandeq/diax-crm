namespace Diax.Application.PromptGenerator.Dtos;

/// <summary>
/// DTO simplificado para listagem de histórico de prompts.
/// </summary>
public record UserPromptHistoryDto(
    Guid Id,
    string InputPreview,
    string PromptType,
    string Provider,
    string? Model,
    DateTime CreatedAt
);

/// <summary>
/// DTO detalhado de um prompt.
/// </summary>
public record UserPromptDetailDto(
    Guid Id,
    string OriginalInput,
    string GeneratedPrompt,
    string PromptType,
    string Provider,
    string? Model,
    DateTime CreatedAt
);
