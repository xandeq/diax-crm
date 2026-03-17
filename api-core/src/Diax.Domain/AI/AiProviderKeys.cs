namespace Diax.Domain.AI;

/// <summary>
/// Central registry of all AI provider keys.
/// Use these constants instead of magic strings when referencing providers.
/// This ensures compile-time safety and makes refactoring easier.
/// </summary>
public static class AiProviderKeys
{
    public const string OpenAi = "openai";
    public const string OpenRouter = "openrouter";
    public const string Gemini = "gemini";
    public const string FalAi = "falai";
    public const string Grok = "grok";
    public const string HuggingFace = "huggingface";
    public const string Groq = "groq";
    public const string Cerebras = "cerebras";
    public const string Anthropic = "anthropic";
    public const string DeepSeek = "deepseek";
    public const string Perplexity = "perplexity";
    public const string Replicate = "replicate";
    public const string TogetherAi = "togetherai";
    public const string Ollama = "ollama";
}
