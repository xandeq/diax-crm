namespace Diax.Application.AI;

/// <summary>
/// Valida providers e modelos de IA consultando o banco de dados (com cache).
/// Fonte única de verdade: banco de dados.
/// </summary>
public interface IAiModelValidator
{
    /// <summary>
    /// Verifica se um provider existe e está ativo.
    /// </summary>
    Task<bool> IsValidProviderAsync(string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se um modelo existe, pertence ao provider informado e está ativo.
    /// </summary>
    Task<bool> IsValidModelAsync(string providerKey, string modelKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a lista de model keys ativos para um provider.
    /// </summary>
    Task<IReadOnlyList<string>> GetActiveModelKeysAsync(string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a lista de provider keys ativos.
    /// </summary>
    Task<IReadOnlyList<string>> GetActiveProviderKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalida o cache, forçando recarga do banco na próxima consulta.
    /// </summary>
    void InvalidateCache();
}
