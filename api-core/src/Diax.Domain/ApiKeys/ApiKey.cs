using System.Security.Cryptography;
using System.Text;
using Diax.Domain.Common;

namespace Diax.Domain.ApiKeys;

/// <summary>
/// Representa uma API Key para autenticação de integrações externas (ex: N8N).
/// </summary>
public class ApiKey : AuditableEntity
{
    /// <summary>
    /// Nome descritivo da API Key (ex: "N8N Blog Automation").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Hash SHA256 da chave (nunca armazenar plaintext).
    /// </summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se a API Key está ativa.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Data de expiração da chave (opcional).
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Data do último uso da chave.
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Escopo de permissões (ex: "Blog", "Blog,Customers").
    /// </summary>
    public string Scope { get; private set; } = string.Empty;

    // Construtor privado para EF Core
    private ApiKey() : base() { }

    /// <summary>
    /// Cria uma nova API Key e retorna a chave plaintext (apenas neste momento).
    /// </summary>
    /// <param name="name">Nome descritivo da chave</param>
    /// <param name="createdBy">Identificador do usuário que criou</param>
    /// <param name="validityDays">Dias de validade (padrão: 90)</param>
    /// <param name="scope">Escopo de permissões (padrão: "Blog")</param>
    /// <returns>Tupla com a entidade ApiKey e a chave plaintext</returns>
    public static (ApiKey apiKey, string plainKey) Create(
        string name,
        string createdBy,
        int validityDays = 90,
        string scope = "Blog")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da API Key é obrigatório.", nameof(name));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Criador da API Key é obrigatório.", nameof(createdBy));

        if (validityDays <= 0)
            throw new ArgumentException("Dias de validade devem ser maior que zero.", nameof(validityDays));

        var plainKey = GenerateSecureKey();
        var keyHash = HashKey(plainKey);

        var apiKey = new ApiKey
        {
            Name = name,
            KeyHash = keyHash,
            IsEnabled = true,
            ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
            Scope = scope
        };

        apiKey.SetCreatedBy(createdBy);

        return (apiKey, plainKey);
    }

    /// <summary>
    /// Desabilita a API Key.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Habilita a API Key.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Registra o uso da API Key.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida se a API Key é válida.
    /// </summary>
    /// <param name="plainKey">Chave plaintext para validação</param>
    /// <returns>True se a chave é válida, False caso contrário</returns>
    public bool IsValid(string plainKey)
    {
        if (!IsEnabled)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return VerifyKey(plainKey, KeyHash);
    }

    /// <summary>
    /// Gera uma chave aleatória segura de 32 caracteres (formato: dk_live_XXXXX).
    /// </summary>
    private static string GenerateSecureKey()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var randomBytes = new byte[24]; // 24 bytes = 32 chars base64-like

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var key = new StringBuilder(32);
        foreach (var b in randomBytes)
        {
            key.Append(chars[b % chars.Length]);
        }

        return $"dk_live_{key}";
    }

    /// <summary>
    /// Gera hash SHA256 da chave.
    /// </summary>
    public static string HashKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave não pode ser vazia.", nameof(key));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifica se a chave plaintext corresponde ao hash armazenado.
    /// </summary>
    private static bool VerifyKey(string plainKey, string hash)
    {
        if (string.IsNullOrWhiteSpace(plainKey) || string.IsNullOrWhiteSpace(hash))
            return false;

        var computedHash = HashKey(plainKey);
        return computedHash == hash;
    }
}
