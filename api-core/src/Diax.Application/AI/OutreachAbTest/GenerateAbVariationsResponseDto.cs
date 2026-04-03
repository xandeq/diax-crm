namespace Diax.Application.AI.OutreachAbTest;

public record GenerateAbVariationsResponseDto(
    List<OutreachVariationDto> Variations,
    string ProviderUsed,
    string ModelUsed,
    DateTime GeneratedAt,
    string RequestId
);

public record OutreachVariationDto(
    string Label,
    string Tone,
    string Subject,
    string Body,
    decimal EstimatedResponseRate,
    string Rationale
);
