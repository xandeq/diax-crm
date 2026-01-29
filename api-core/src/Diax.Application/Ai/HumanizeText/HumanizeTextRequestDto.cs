namespace Diax.Application.Ai.HumanizeText;

public record HumanizeTextRequestDto(
    string Provider,
    string Tone,
    string InputText,
    string? Language = "pt-BR",
    double? Temperature = null,
    int? MaxTokens = null
);
