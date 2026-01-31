namespace Diax.Application.PromptGenerator;

public class PromptGeneratorSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public ProviderConfig OpenAI { get; set; } = new();
    public ProviderConfig Perplexity { get; set; } = new();
    public ProviderConfig DeepSeek { get; set; } = new();
    public ProviderConfig Gemini { get; set; } = new();
}

public class ProviderConfig
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? Model { get; set; }
}
