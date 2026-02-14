namespace Diax.Application.ApiKeys.Dtos;

/// <summary>
/// Request para criar uma nova API Key.
/// </summary>
public record CreateApiKeyRequest
{
    /// <summary>
    /// Nome descritivo da API Key (ex: "N8N Blog Automation").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Dias de validade da chave (padrão: 90 dias).
    /// </summary>
    public int ValidityDays { get; init; } = 90;

    /// <summary>
    /// Escopo de permissões (padrão: "Blog").
    /// </summary>
    public string Scope { get; init; } = "Blog";
}
