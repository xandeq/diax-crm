using Diax.Application.AI.ImageGeneration.Dtos;

namespace Diax.Application.AI.ImageGeneration;

public interface IImageGenerationService
{
    Task<ImageGenerationResponseDto> GenerateAsync(
        ImageGenerationRequestDto request,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>Histórico de imagens do usuário (mais recentes primeiro; só itens com URL utilizável).</summary>
    Task<List<ImageHistoryItemDto>> ListImagesAsync(Guid userId, int take = 24, CancellationToken ct = default);
}
