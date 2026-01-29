namespace Diax.Application.Ai.HumanizeText;

public interface IHumanizeTextService
{
    Task<HumanizeTextResponseDto> HumanizeAsync(HumanizeTextRequestDto request, CancellationToken ct = default);
}
