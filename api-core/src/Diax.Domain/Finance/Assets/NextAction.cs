using Diax.Domain.Common;

namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Próxima ação recomendada do módulo Patrimônio (F2 — gap engine).
/// Recomendações de aporte/rebalanceamento geradas a partir da alocação atual vs meta.
/// </summary>
public class NextAction : AuditableEntity, IUserOwnedEntity
{
    public const string CategoryAporte = "aporte";
    public const string CategoryDiversificar = "diversificar";
    public const string CategoryRebalancear = "rebalancear";
    public const string CategoryResgatar = "resgatar";
    public const string CategoryAdquirir = "adquirir";

    public const string StatusPending = "pending";
    public const string StatusDone = "done";
    public const string StatusDismissed = "dismissed";

    private static readonly string[] ValidCategories =
    {
        CategoryAporte, CategoryDiversificar, CategoryRebalancear, CategoryResgatar, CategoryAdquirir
    };

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Rationale { get; private set; } = string.Empty;

    /// <summary>
    /// Categoria da ação: "aporte" | "diversificar" | "rebalancear" | "resgatar" | "adquirir".
    /// </summary>
    public string Category { get; private set; } = CategoryAporte;

    public decimal? SuggestedAmount { get; private set; }
    public AssetClass? TargetClass { get; private set; }

    /// <summary>
    /// Status da ação: "pending" | "done" | "dismissed".
    /// </summary>
    public string Status { get; private set; } = StatusPending;

    public Guid? LinkedTaskId { get; private set; }

    private NextAction() { }

    public static NextAction Create(
        Guid userId,
        string title,
        string rationale,
        string category,
        decimal? suggestedAmount = null,
        AssetClass? targetClass = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Action title cannot be empty", nameof(title));

        if (title.Length > 200)
            throw new ArgumentException("Action title cannot exceed 200 characters", nameof(title));

        if (!ValidCategories.Contains(category))
            throw new ArgumentException($"Invalid category '{category}'", nameof(category));

        return new NextAction
        {
            UserId = userId,
            Title = title,
            Rationale = rationale ?? string.Empty,
            Category = category,
            SuggestedAmount = suggestedAmount,
            TargetClass = targetClass,
            Status = StatusPending
        };
    }

    /// <summary>
    /// Marca a ação como concluída, opcionalmente vinculando uma tarefa do CRM.
    /// </summary>
    public void Complete(Guid? linkedTaskId = null)
    {
        Status = StatusDone;
        if (linkedTaskId.HasValue)
        {
            LinkedTaskId = linkedTaskId;
        }
        SetUpdated();
    }

    /// <summary>
    /// Descarta a ação (não será mais sugerida).
    /// </summary>
    public void Dismiss()
    {
        Status = StatusDismissed;
        SetUpdated();
    }
}
