using Diax.Domain.EmailMarketing;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class EmailSuppressionRepository : Repository<EmailSuppression>, IEmailSuppressionRepository
{
    public EmailSuppressionRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<bool> IsSuppressedAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var atIndex = normalizedEmail.IndexOf('@');
        var domain = atIndex >= 0 ? normalizedEmail[(atIndex + 1)..] : string.Empty;

        return await DbSet.AnyAsync(s =>
            s.UserId == userId &&
            (
                (s.Email != null && s.Email == normalizedEmail) ||
                (s.DomainPattern != null && domain != string.Empty && domain == s.DomainPattern)
            ),
            cancellationToken);
    }

    public async Task<List<EmailSuppression>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SuppressedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(EmailSuppression suppression, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(suppression, cancellationToken);

    public async Task<EmailSuppression?> FindByEmailAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await DbSet.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Email == normalizedEmail,
            cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet.FindAsync([id], cancellationToken);
        if (entity != null)
            DbSet.Remove(entity);
    }
}
