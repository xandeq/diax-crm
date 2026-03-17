namespace Diax.Application.AI.EmailOptimization;

public record GenerateSubjectLinesRequestDto(
    string Provider,
    string? Model,
    string BaseMessage,
    string? TargetAudience = null,
    double? Temperature = null,
    int? MaxTokens = null
);
