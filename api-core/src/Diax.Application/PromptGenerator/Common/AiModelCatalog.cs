namespace Diax.Application.PromptGenerator.Common;

public static class AiModelCatalog
{
    // ===== CATÁLOGO ESPECÍFICO DO GEMINI =====
    // Modelos oficialmente aprovados para uso com generateContent
    public static readonly List<GeminiModelDto> GeminiModels = new()
    {
        // Stable (Default)
        new("models/gemini-2.5-flash", "Gemini 2.5 Flash", "Stable",
            new() { "generateContent", "countTokens" }, IsDefault: true),

        // Stable Alternatives
        new("models/gemini-2.0-flash", "Gemini 2.0 Flash", "Stable",
            new() { "generateContent", "countTokens" }),
        new("models/gemini-flash-latest", "Gemini Flash (Latest)", "Stable",
            new() { "generateContent", "countTokens" }),
        new("models/gemini-pro-latest", "Gemini Pro (Latest)", "Stable",
            new() { "generateContent", "countTokens" }),

        // Economy (Gemma models)
        new("models/gemma-3-4b-it", "Gemma 3 4B IT", "Economy",
            new() { "generateContent" }),
        new("models/gemma-3-12b-it", "Gemma 3 12B IT", "Economy",
            new() { "generateContent" })
    };

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
        }),
        new("gemini", "Google Gemini", GeminiModels
            .Where(m => m.SupportsGenerateContent)
            .Select(m => new AiModelDto(m.Name, m.DisplayName, m.Category, m.IsDefault))
            .ToList()),
        new("openrouter", "OpenRouter", new List<AiModelDto>
        {
            new("openai/gpt-4o-mini", "GPT-4o Mini", "Standard", true),
            new("openai/gpt-4o", "GPT-4o", "Standard"),
            new("mistralai/mistral-large", "Mistral Large", "Standard"),
            new("google/gemini-flash-1.5", "Gemini 1.5 Flash", "Standard"),
            new("openai/o1", "o1", "Reasoning"),
            new("anthropic/claude-3.5-sonnet", "Claude 3.5 Sonnet", "Reasoning"),
            new("deepseek/deepseek-r1", "DeepSeek R1", "Reasoning")
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

    // ===== MÉTODOS ESPECÍFICOS DO GEMINI =====

    /// <summary>
    /// Valida se um modelo Gemini existe e suporta generateContent.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateGeminiModel(string modelId)
    {
        var model = GeminiModels.FirstOrDefault(m => m.Name == modelId);

        if (model == null)
        {
            var availableModels = string.Join(", ", GeminiModels.Select(m => m.Name));
            return (false, $"Modelo Gemini '{modelId}' não encontrado. Modelos disponíveis: {availableModels}");
        }

        if (!model.SupportsGenerateContent)
        {
            return (false, $"Modelo Gemini '{modelId}' não suporta geração de texto (generateContent).");
        }

        return (true, null);
    }

    /// <summary>
    /// Obtém o modelo Gemini padrão.
    /// </summary>
    public static string GetDefaultGeminiModel()
    {
        return GeminiModels.FirstOrDefault(m => m.IsDefault)?.Name
               ?? GeminiModels.First().Name;
    }
}
