namespace Diax.Application.PromptGenerator.Common;

public static class AiModelCatalog
{
    public static readonly List<ProviderModelsDto> Providers = new()
    {
        new("chatgpt", "OpenAI", new List<AiModelDto>
        {
            new("gpt-4o-mini", "GPT-4o Mini", "Standard", true),
            new("gpt-4o", "GPT-4o", "Standard"),
            new("o1-preview", "o1 Preview", "Reasoning"),
            new("o1-mini", "o1 Mini", "Reasoning"),
            new("o3-mini", "o3 Mini", "Reasoning")
        }),
        new("deepseek", "DeepSeek", new List<AiModelDto>
        {
            new("deepseek-chat", "DeepSeek Chat (V3)", "Standard", true),
            new("deepseek-reasoner", "DeepSeek Reasoner (R1)", "Reasoning")
        }),
        new("perplexity", "Perplexity", new List<AiModelDto>
        {
            new("sonar", "Sonar", "Standard"),
            new("sonar-pro", "Sonar Pro", "Standard", true),
            new("sonar-reasoning", "Sonar Reasoning", "Reasoning"),
            new("sonar-reasoning-pro", "Sonar Reasoning Pro", "Reasoning")
        })
    };

    public static string GetDefaultModel(string providerId)
    {
        var provider = Providers.FirstOrDefault(p => p.ProviderId == providerId);
        return provider?.Models.FirstOrDefault(m => m.IsDefault)?.Id
               ?? provider?.Models.FirstOrDefault()?.Id
               ?? string.Empty;
    }

    public static bool IsModelValid(string providerId, string modelId)
    {
        return Providers.Any(p => p.ProviderId == providerId && p.Models.Any(m => m.Id == modelId));
    }
}
