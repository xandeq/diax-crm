using Diax.Domain.Common;

namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Perfil patrimonial do usuário (F2): perfil de risco, meta de patrimônio e alocação-alvo.
/// Um registro por usuário (índice único em user_id).
/// </summary>
public class WealthProfile : AuditableEntity, IUserOwnedEntity
{
    public const string DefaultRiskProfile = "builder_all";

    public Guid UserId { get; private set; }

    /// <summary>
    /// Perfil de risco (ex: "builder_all").
    /// </summary>
    public string RiskProfile { get; private set; } = DefaultRiskProfile;

    /// <summary>
    /// Meta de patrimônio em BRL (ex: 1.000.000).
    /// </summary>
    public decimal? GoalAmount { get; private set; }

    /// <summary>
    /// Horizonte da meta em anos (ex: 5).
    /// </summary>
    public int? GoalYears { get; private set; }

    /// <summary>
    /// Alocação-alvo por classe, serializada como JSON: {"RendaFixa":25,"Acao":20,...}.
    /// </summary>
    public string? TargetAllocationJson { get; private set; }

    private WealthProfile() { }

    public static WealthProfile Create(
        Guid userId,
        string? riskProfile = null,
        decimal? goalAmount = null,
        int? goalYears = null,
        string? targetAllocationJson = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        var profile = new WealthProfile { UserId = userId };
        profile.UpdateGoal(riskProfile ?? DefaultRiskProfile, goalAmount, goalYears);
        if (targetAllocationJson != null)
        {
            profile.UpdateAllocation(targetAllocationJson);
        }

        return profile;
    }

    /// <summary>
    /// Atualiza perfil de risco e meta (valor + horizonte).
    /// </summary>
    public void UpdateGoal(string riskProfile, decimal? goalAmount, int? goalYears)
    {
        if (string.IsNullOrWhiteSpace(riskProfile))
            throw new ArgumentException("Risk profile cannot be empty", nameof(riskProfile));

        if (riskProfile.Length > 50)
            throw new ArgumentException("Risk profile cannot exceed 50 characters", nameof(riskProfile));

        if (goalAmount.HasValue && goalAmount.Value <= 0)
            throw new ArgumentException("Goal amount must be positive", nameof(goalAmount));

        if (goalYears.HasValue && goalYears.Value <= 0)
            throw new ArgumentException("Goal years must be positive", nameof(goalYears));

        RiskProfile = riskProfile;
        GoalAmount = goalAmount;
        GoalYears = goalYears;
        SetUpdated();
    }

    /// <summary>
    /// Atualiza a alocação-alvo (JSON por classe).
    /// </summary>
    public void UpdateAllocation(string targetAllocationJson)
    {
        if (string.IsNullOrWhiteSpace(targetAllocationJson))
            throw new ArgumentException("Target allocation cannot be empty", nameof(targetAllocationJson));

        TargetAllocationJson = targetAllocationJson;
        SetUpdated();
    }
}
