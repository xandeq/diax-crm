using Diax.Domain.Common;
using Diax.Domain.Auth;
using Diax.Domain.EmailMarketing;

namespace Diax.Domain.AI;

public class EmailOptimization : AuditableEntity
{
    public new long Id { get; set; }
    public Guid UserId { get; set; }
    public long? EmailCampaignId { get; set; }
    public string OriginalSubject { get; set; } = null!;
    public string GeneratedSubjectsJson { get; set; } = null!;
    public string? SelectedSubject { get; set; }
    public decimal? EstimatedOpenRateImprovement { get; set; }
    public bool? ActualOpenRateImproved { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual EmailCampaign? EmailCampaign { get; set; }
}
