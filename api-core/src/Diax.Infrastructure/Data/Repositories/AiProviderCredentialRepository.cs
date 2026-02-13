using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiProviderCredentialRepository : Repository<AiProviderCredential>, IAiProviderCredentialRepository
{
    private readonly DiaxDbContext _context;

    public AiProviderCredentialRepository(DiaxDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<AiProviderCredential?> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.ProviderId == providerId, cancellationToken);
    }

    public async Task<AiProviderCredential> CreateOrUpdateAsync(
        Guid providerId,
        string encryptedKey,
        string lastFourDigits,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByProviderIdAsync(providerId, cancellationToken);

        if (existing != null)
        {
            // Atualiza credencial existente
            existing.UpdateApiKey(encryptedKey, lastFourDigits);
            DbSet.Update(existing);
            return existing;
        }
        else
        {
            // Cria nova credencial
            var credential = new AiProviderCredential(providerId, encryptedKey, lastFourDigits);
            await DbSet.AddAsync(credential, cancellationToken);
            return credential;
        }
    }

    public async Task<bool> HasCredentialAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(c => c.ProviderId == providerId && c.ApiKeyEncrypted != "", cancellationToken);
    }
}
