using Diax.Domain.Common;

namespace Diax.Domain.EmailMarketing;

public interface IEmailCampaignRepository : IRepository<EmailCampaign>
{
    Task<(IEnumerable<EmailCampaign> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task IncrementSentAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task IncrementFailedAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>Desfaz uma contagem de Failed quando o item volta para retry.</summary>
    Task DecrementFailedAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
