namespace Diax.Application.EmailMarketing.Pro.Dtos;

public record NormalizationStatsDto
{
    public int Total { get; init; }
    public int Normalized { get; init; }
    public int Pending { get; init; }
    public double CoveragePercent { get; init; }
    public int HighConfidence { get; init; }
    public int LowConfidence { get; init; }
}
