namespace Diax.Application.EmailMarketing.Pro.Dtos;

public record NormalizationPreviewItemDto
{
    public string CustomerId { get; init; } = "";
    public string RawName { get; init; } = "";
    public string? Email { get; init; }
    public string? SuggestedName { get; init; }
    public int SuggestedScore { get; init; }
    public string SuggestedSource { get; init; } = "";
    public string? CurrentNormalizedName { get; init; }
    public int? CurrentScore { get; init; }
}
