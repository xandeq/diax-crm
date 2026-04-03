namespace Diax.Application.AI.SocialMediaBatch;

public record GenerateSocialBatchRequestDto(
    string Provider,
    string? Model,
    List<string> Topics,
    List<string> Platforms,
    int PostCount = 15,
    string? Month = null,
    string? BrandVoice = null,
    string? TargetAudience = null,
    double? Temperature = null,
    int? MaxTokens = null
);
