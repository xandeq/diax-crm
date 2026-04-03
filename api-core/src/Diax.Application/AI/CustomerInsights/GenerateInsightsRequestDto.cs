namespace Diax.Application.AI.CustomerInsights;

public record GenerateInsightsRequestDto(
    string Provider,
    string? Model,
    string? DateRange = "last_30_days",
    string? FocusArea = null,
    double? Temperature = null,
    int? MaxTokens = null
);
