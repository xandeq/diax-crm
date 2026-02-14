using Diax.Domain.ApiKeys;

namespace Diax.Application.ApiKeys.Dtos;

/// <summary>
/// Response representando uma API Key (sem expor a chave plaintext).
/// </summary>
public record ApiKeyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public string Scope { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Converte entidade ApiKey em DTO de resposta (sem expor KeyHash).
    /// </summary>
    public static ApiKeyResponse FromEntity(ApiKey apiKey)
    {
        return new ApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            IsEnabled = apiKey.IsEnabled,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            Scope = apiKey.Scope,
            CreatedAt = apiKey.CreatedAt,
            CreatedBy = apiKey.CreatedBy
        };
    }
}

/// <summary>
/// Response para criação de API Key (retorna plaintext APENAS uma vez).
/// </summary>
public record CreateApiKeyResponse : ApiKeyResponse
{
    /// <summary>
    /// Chave plaintext (retornada APENAS na criação).
    /// </summary>
    public string PlainKey { get; init; } = string.Empty;

    public static CreateApiKeyResponse FromEntityWithKey(ApiKey apiKey, string plainKey)
    {
        var baseResponse = FromEntity(apiKey);
        return new CreateApiKeyResponse
        {
            Id = baseResponse.Id,
            Name = baseResponse.Name,
            IsEnabled = baseResponse.IsEnabled,
            ExpiresAt = baseResponse.ExpiresAt,
            LastUsedAt = baseResponse.LastUsedAt,
            Scope = baseResponse.Scope,
            CreatedAt = baseResponse.CreatedAt,
            CreatedBy = baseResponse.CreatedBy,
            PlainKey = plainKey
        };
    }
}
