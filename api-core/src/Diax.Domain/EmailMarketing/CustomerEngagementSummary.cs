namespace Diax.Domain.EmailMarketing;

/// <summary>
/// Agregado de engajamento de email por cliente — insumo do lead scoring.
/// </summary>
public record CustomerEngagementSummary(
    Guid CustomerId,
    int OpenCount,
    int ClickCount,
    int BounceCount,
    DateTime? LastEngagementAt   // último Opened/Clicked
);
