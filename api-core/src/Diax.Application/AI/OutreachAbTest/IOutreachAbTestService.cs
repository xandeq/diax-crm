namespace Diax.Application.AI.OutreachAbTest;

public interface IOutreachAbTestService
{
    Task<GenerateAbVariationsResponseDto> GenerateVariationsAsync(
        GenerateAbVariationsRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
