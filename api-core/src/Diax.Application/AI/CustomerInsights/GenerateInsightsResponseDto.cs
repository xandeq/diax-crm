namespace Diax.Application.AI.CustomerInsights;

public record GenerateInsightsResponseDto(
    string Title,
    InsightsSummaryDto Summary,
    List<InsightPatternDto> Patterns,
    List<string> Recommendations,
    string ProviderUsed,
    string ModelUsed,
    DateTime GeneratedAt,
    string RequestId
);

public record InsightsSummaryDto(
    int TotalLeads,
    int TotalCustomers,
    int NewLeadsInPeriod,
    int ConvertedInPeriod,
    decimal ConversionRate,
    string TopSource,
    string TopSegment
);

public record InsightPatternDto(
    string Title,
    string Description,
    string Impact,
    string Category
);
