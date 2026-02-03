using Diax.Domain.Common;

namespace Diax.Domain.PromptGenerator;

/// <summary>
/// Entidade que representa um prompt gerado e salvo por um usuário.
/// </summary>
public class UserPrompt : Entity
{
    /// <summary>
    /// ID do usuário que gerou o prompt.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Texto original digitado pelo usuário.
    /// </summary>
    public string OriginalInput { get; private set; } = string.Empty;

    /// <summary>
    /// Prompt final gerado pelo sistema.
    /// </summary>
    public string GeneratedPrompt { get; private set; } = string.Empty;

    /// <summary>
    /// Tipo/técnica de prompt utilizada (ex: "aida", "pas", "professional").
    /// </summary>
    public string PromptType { get; private set; } = string.Empty;

    /// <summary>
    /// Provider de IA selecionado (ex: "chatgpt", "gemini", "deepseek").
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Modelo específico utilizado (ex: "gpt-4o-mini", "gemini-1.5-pro").
    /// </summary>
    public string? Model { get; private set; }

    /// <summary>
    /// Data e hora de criação (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private UserPrompt() { }

    /// <summary>
    /// Cria um novo UserPrompt.
    /// </summary>
    public UserPrompt(
        Guid userId,
        string originalInput,
        string generatedPrompt,
        string promptType,
        string provider,
        string? model = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(originalInput))
            throw new ArgumentException("OriginalInput is required.", nameof(originalInput));

        if (string.IsNullOrWhiteSpace(generatedPrompt))
            throw new ArgumentException("GeneratedPrompt is required.", nameof(generatedPrompt));

        UserId = userId;
        OriginalInput = originalInput.Trim();
        GeneratedPrompt = generatedPrompt;
        PromptType = promptType?.Trim().ToLowerInvariant() ?? "professional";
        Provider = provider?.Trim().ToLowerInvariant() ?? "chatgpt";
        Model = model?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Retorna um preview truncado do input original.
    /// </summary>
    public string GetInputPreview(int maxLength = 100)
    {
        if (string.IsNullOrEmpty(OriginalInput))
            return string.Empty;

        if (OriginalInput.Length <= maxLength)
            return OriginalInput;

        return OriginalInput[..maxLength] + "...";
    }
}
