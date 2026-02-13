using Diax.Domain.Common;

namespace Diax.Domain.AI;

/// <summary>
/// Armazena credenciais criptografadas para provedores de IA.
/// API keys são criptografadas usando ASP.NET Core Data Protection API.
/// </summary>
public class AiProviderCredential : AuditableEntity
{
    public new Guid Id { get; private set; }

    /// <summary>
    /// ID do provedor de IA (foreign key para AiProvider)
    /// </summary>
    public Guid ProviderId { get; private set; }

    /// <summary>
    /// API Key criptografada usando Data Protection API
    /// </summary>
    public string ApiKeyEncrypted { get; private set; } = string.Empty;

    /// <summary>
    /// Últimos 4 dígitos da API key (para exibição na UI)
    /// </summary>
    public string ApiKeyLastFourDigits { get; private set; } = string.Empty;

    // Navigation property
    public AiProvider Provider { get; private set; } = null!;

    // EF Core constructor
    private AiProviderCredential() { }

    public AiProviderCredential(Guid providerId, string apiKeyEncrypted, string apiKeyLastFourDigits)
    {
        Id = Guid.NewGuid();
        ProviderId = providerId;
        ApiKeyEncrypted = apiKeyEncrypted;
        ApiKeyLastFourDigits = apiKeyLastFourDigits;
    }

    /// <summary>
    /// Atualiza a API key criptografada
    /// </summary>
    public void UpdateApiKey(string encryptedKey, string lastFourDigits)
    {
        if (string.IsNullOrWhiteSpace(encryptedKey))
            throw new ArgumentException("Encrypted key cannot be empty", nameof(encryptedKey));

        ApiKeyEncrypted = encryptedKey;
        ApiKeyLastFourDigits = lastFourDigits ?? string.Empty;
    }

    /// <summary>
    /// Verifica se a credencial está configurada (possui API key)
    /// </summary>
    public bool IsConfigured() => !string.IsNullOrWhiteSpace(ApiKeyEncrypted);
}
