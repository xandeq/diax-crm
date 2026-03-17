namespace Diax.Application.PromptGenerator;

public class PromptGeneratorSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public ProviderConfig OpenAI { get; set; } = new();
    public ProviderConfig Perplexity { get; set; } = new();
    public ProviderConfig DeepSeek { get; set; } = new();
    public ProviderConfig Gemini { get; set; } = new();
    public ProviderConfig OpenRouter { get; set; } = new();
    public ProviderConfig FAL { get; set; } = new();
    public ProviderConfig Grok { get; set; } = new();
    public ProviderConfig HuggingFace { get; set; } = new();
    public ProviderConfig Groq { get; set; } = new();
    public ProviderConfig Cerebras { get; set; } = new();
    public ProviderConfig Anthropic { get; set; } = new();

    /// <summary>
    /// Obtém a configuração do provider pelo key do banco de dados.
    /// Mapeamento: chatgpt -> OpenAI, openai -> OpenAI, etc.
    /// </summary>
    public ProviderConfig? GetProviderConfig(string providerKey)
    {
        return providerKey?.ToLowerInvariant() switch
        {
            "chatgpt" or "openai" => OpenAI,
            "perplexity" => Perplexity,
            "deepseek" => DeepSeek,
            "gemini" => Gemini,
            "openrouter" => OpenRouter,
            "fal" or "falai" => FAL,
            "grok" or "xai" => Grok,
            "huggingface" or "hf" or "hugging-face" => HuggingFace,
            "groq" => Groq,
            "cerebras" => Cerebras,
            "anthropic" or "claude" => Anthropic,
            _ => null
        };
    }

    /// <summary>
    /// Verifica se existe configuração válida (com ApiKey) para o provider.
    /// </summary>
    public bool HasConfiguredApiKey(string providerKey)
    {
        var config = GetProviderConfig(providerKey);
        return !string.IsNullOrWhiteSpace(config?.ApiKey);
    }
}

public class ProviderConfig
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? Model { get; set; }
}
