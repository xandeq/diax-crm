namespace Diax.Application.PromptGenerator.Dtos;

public class GeneratePromptRequestDto
{
    public string RawPrompt { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? PromptType { get; set; }
}
