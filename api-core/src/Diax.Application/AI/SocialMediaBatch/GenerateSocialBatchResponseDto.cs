namespace Diax.Application.AI.SocialMediaBatch;

public record GenerateSocialBatchResponseDto(
    List<SocialPostDto> Posts,
    string ProviderUsed,
    string ModelUsed,
    DateTime GeneratedAt,
    string RequestId
);

public record SocialPostDto(
    int Number,
    string Platform,
    string ContentType,
    string Caption,
    List<string> Hashtags,
    string? ImagePrompt,
    string ImageDimension,
    string BestTimeToPost,
    string Topic
);
