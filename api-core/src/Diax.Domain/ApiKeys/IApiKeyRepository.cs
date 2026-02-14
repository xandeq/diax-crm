using Diax.Domain.Common;

namespace Diax.Domain.ApiKeys;

/// <summary>
/// Repositório para operações com API Keys.
/// </summary>
public interface IApiKeyRepository : IRepository<ApiKey>
{
    /// <summary>
    /// Obtém uma API Key pelo hash da chave.
    /// </summary>
    /// <param name="keyHash">Hash SHA256 da chave</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>API Key encontrada ou null</returns>
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as API Keys ativas (não expiradas e habilitadas).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de API Keys ativas</returns>
    Task<IEnumerable<ApiKey>> GetActiveKeysAsync(CancellationToken cancellationToken = default);
}
