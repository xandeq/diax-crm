using Diax.Application.AI.VideoGeneration.Dtos;

namespace Diax.Application.AI.VideoGeneration;

public interface IVideoGenerationService
{
    Task<VideoGenerationResponseDto> GenerateAsync(
        VideoGenerationRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
