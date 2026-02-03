namespace Diax.Application.PromptGenerator.Common;

public record AiModelDto(string Id, string Name, string Category, bool IsDefault = false);

public record ProviderModelsDto(string ProviderId, string ProviderName, List<AiModelDto> Models);

/// <summary>
/// DTO específico para modelos Gemini com informação de métodos suportados.
/// </summary>
public record GeminiModelDto(
    string Name,
    string DisplayName,
    string Category,
    List<string> SupportedGenerationMethods,
    bool IsDefault = false)
{
    /// <summary>
    /// Verifica se o modelo suporta o método generateContent.
    /// </summary>
    public bool SupportsGenerateContent => SupportedGenerationMethods.Contains("generateContent");
}
