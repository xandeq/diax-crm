using Diax.Domain.Common;

namespace Diax.Domain.AI;

public class AiModel : AuditableEntity
{
    public Guid ProviderId { get; private set; }
    public string ModelKey { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsDiscovered { get; private set; }
    public decimal? InputCostHint { get; private set; }
    public decimal? OutputCostHint { get; private set; }
    public int? MaxTokensHint { get; private set; }
    public string? CapabilitiesJson { get; private set; }

    // Navigation property
    public AiProvider Provider { get; private set; }

    public AiModel(Guid providerId, string modelKey, string displayName, bool isDiscovered)
    {
        ProviderId = providerId;
        ModelKey = modelKey;
        DisplayName = displayName;
        IsDiscovered = isDiscovered;
        IsEnabled = false; // Default disabled
    }

    public void UpdateDetails(string displayName, decimal? inputCost, decimal? outputCost, int? maxTokens, string? capabilities)
    {
        DisplayName = displayName;
        InputCostHint = inputCost;
        OutputCostHint = outputCost;
        MaxTokensHint = maxTokens;
        CapabilitiesJson = capabilities;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
