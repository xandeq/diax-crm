namespace Diax.Application.AI.EmailOptimization;

public record GenerateSubjectLinesResponseDto(
    List<SubjectLineDto> SubjectLines,
    string ProviderUsed,
    string ModelUsed,
    DateTime GeneratedAt,
    string RequestId
);
