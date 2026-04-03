namespace Diax.Application.AI.SocialMediaBatch;

public interface ISocialMediaBatchService
{
    Task<GenerateSocialBatchResponseDto> GenerateBatchAsync(
        GenerateSocialBatchRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
