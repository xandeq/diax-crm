namespace Diax.Application.Ai.HumanizeText;

public record HumanizeTextResponseDto(
    string OutputText,
    string ProviderUsed,
    string ToneUsed,
    string RequestId
);
