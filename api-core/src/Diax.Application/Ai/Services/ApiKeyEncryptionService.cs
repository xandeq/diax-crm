using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI.Services;

/// <summary>
/// Serviço de criptografia/descriptografia de API keys usando ASP.NET Core Data Protection API.
///
/// IMPORTANTE:
/// - As chaves de criptografia são persistidas automaticamente pelo Data Protection
/// - Em produção, configure um diretório persistente (não appdomain) ou use Azure Key Vault
/// - API keys NUNCA devem ser logadas (nem parciais)
/// </summary>
public interface IApiKeyEncryptionService
{
    /// <summary>
    /// Criptografa uma API key em plain text
    /// </summary>
    string Encrypt(string plainKey);

    /// <summary>
    /// Descriptografa uma API key criptografada
    /// </summary>
    string Decrypt(string encryptedKey);

    /// <summary>
    /// Extrai os últimos 4 caracteres de uma API key (para exibição na UI)
    /// </summary>
    string GetLastFourDigits(string plainKey);
}

public class ApiKeyEncryptionService : IApiKeyEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<ApiKeyEncryptionService> _logger;

    private const string Purpose = "Diax.AiProviderApiKeys";

    public ApiKeyEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ApiKeyEncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Encrypt(string plainKey)
    {
        if (string.IsNullOrWhiteSpace(plainKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(plainKey));
        }

        try
        {
            var encrypted = _protector.Protect(plainKey);
            _logger.LogInformation("[ApiKeyEncryption] API key encrypted successfully");
            return encrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiKeyEncryption] Failed to encrypt API key");
            throw new InvalidOperationException("Failed to encrypt API key", ex);
        }
    }

    public string Decrypt(string encryptedKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedKey))
        {
            throw new ArgumentException("Encrypted key cannot be empty", nameof(encryptedKey));
        }

        try
        {
            var decrypted = _protector.Unprotect(encryptedKey);
            _logger.LogInformation("[ApiKeyEncryption] API key decrypted successfully");
            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiKeyEncryption] Failed to decrypt API key (key may be invalid or rotated)");
            throw new InvalidOperationException("Failed to decrypt API key", ex);
        }
    }

    public string GetLastFourDigits(string plainKey)
    {
        if (string.IsNullOrWhiteSpace(plainKey))
        {
            return string.Empty;
        }

        return plainKey.Length >= 4
            ? plainKey.Substring(plainKey.Length - 4)
            : new string('*', plainKey.Length);
    }
}
