using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Domain.EmailMarketing;

public interface IEmailSuppressionRepository
{
    Task<bool> IsSuppressedAsync(Guid userId, string email, CancellationToken cancellationToken = default);
    Task<List<EmailSuppression>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(EmailSuppression suppression, CancellationToken cancellationToken = default);
    Task<EmailSuppression?> FindByEmailAsync(Guid userId, string email, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
