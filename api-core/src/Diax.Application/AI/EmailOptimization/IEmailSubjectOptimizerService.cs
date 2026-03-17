namespace Diax.Application.AI.EmailOptimization;

public interface IEmailSubjectOptimizerService
{
    Task<GenerateSubjectLinesResponseDto> GenerateSubjectLinesAsync(
        GenerateSubjectLinesRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
