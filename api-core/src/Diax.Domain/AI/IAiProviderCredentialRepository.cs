using Diax.Domain.Common;

namespace Diax.Domain.AI;

public interface IAiProviderCredentialRepository : IRepository<AiProviderCredential>
{
    /// <summary>
    /// Busca credencial por ID do provider
    /// </summary>
    Task<AiProviderCredential?> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria ou atualiza credencial de um provider
    /// </summary>
    Task<AiProviderCredential> CreateOrUpdateAsync(Guid providerId, string encryptedKey, string lastFourDigits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se provider tem credencial configurada
    /// </summary>
    Task<bool> HasCredentialAsync(Guid providerId, CancellationToken cancellationToken = default);
}
