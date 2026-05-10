namespace Diax.Application.EmailMarketing.Pro.Dtos;

public record RunNormalizationResultDto
{
    public int Processed { get; init; }
    public int Updated { get; init; }
    public int Skipped { get; init; }
}
