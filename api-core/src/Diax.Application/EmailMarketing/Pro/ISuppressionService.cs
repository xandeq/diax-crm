using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Application.EmailMarketing.Pro;

public interface ISuppressionService
{
    Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default);
    Task<List<EmailSuppression>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SuppressEmailAsync(string email, SuppressionReason reason, string source, CancellationToken cancellationToken = default);
    Task SuppressDomainAsync(string domain, SuppressionReason reason, string source, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid suppressionId, CancellationToken cancellationToken = default);
}
