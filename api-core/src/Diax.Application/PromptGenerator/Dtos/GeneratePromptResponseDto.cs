namespace Diax.Application.PromptGenerator.Dtos;

public class GeneratePromptResponseDto
{
    public GeneratePromptResponseDto(string finalPrompt)
    {
        FinalPrompt = finalPrompt;
    }

    public string FinalPrompt { get; set; } = string.Empty;
}
