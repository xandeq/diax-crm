using Diax.Domain.Common;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Domain.EmailMarketing;

public class EmailSuppression : AuditableEntity
{
    public Guid UserId { get; private set; }

    // One of Email or DomainPattern must be set
    public string? Email { get; private set; }
    public string? DomainPattern { get; private set; }

    public SuppressionReason Reason { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public DateTime SuppressedAt { get; private set; }

    protected EmailSuppression() { }

    public static EmailSuppression ForEmail(
        Guid userId,
        string email,
        SuppressionReason reason,
        string source)
    {
        return new EmailSuppression
        {
            UserId = userId,
            Email = email.Trim().ToLowerInvariant(),
            Reason = reason,
            Source = source,
            SuppressedAt = DateTime.UtcNow,
        };
    }

    public static EmailSuppression ForDomain(
        Guid userId,
        string domain,
        SuppressionReason reason,
        string source)
    {
        return new EmailSuppression
        {
            UserId = userId,
            DomainPattern = domain.Trim().ToLowerInvariant(),
            Reason = reason,
            Source = source,
            SuppressedAt = DateTime.UtcNow,
        };
    }
}
