namespace Diax.Application.PromptGenerator;

public interface IPromptGeneratorService
{
    Task<string> GenerateAsync(string rawPrompt, string provider);
}
