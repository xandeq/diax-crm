namespace Diax.Application.Ai.Dtos;

public record AiUsageSummaryRequestDto(
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? ProviderId,
    Guid? ModelId,
    Guid? UserId
);

public record AiUsageSummaryResponseDto(
    int TotalRequests,
    int TotalTokensInput,
    int TotalTokensOutput,
    int TotalTokens,
    decimal TotalCostEstimated,
    Dictionary<string, ProviderUsageDto> ByProvider,
    DateTime? PeriodStart,
    DateTime? PeriodEnd
);

public record ProviderUsageDto(
    Guid ProviderId,
    string ProviderName,
    int RequestCount,
    decimal TotalCost
);
