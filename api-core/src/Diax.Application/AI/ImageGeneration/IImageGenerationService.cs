using Diax.Application.AI.ImageGeneration.Dtos;

namespace Diax.Application.AI.ImageGeneration;

public interface IImageGenerationService
{
    Task<ImageGenerationResponseDto> GenerateAsync(
        ImageGenerationRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
