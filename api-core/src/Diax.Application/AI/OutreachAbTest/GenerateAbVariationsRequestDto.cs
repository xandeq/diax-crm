namespace Diax.Application.AI.OutreachAbTest;

public record GenerateAbVariationsRequestDto(
    string Provider,
    string? Model,
    string BaseMessage,
    string? TargetAudience = null,
    string? Industry = null,
    string? Goal = null,
    double? Temperature = null,
    int? MaxTokens = null
);
