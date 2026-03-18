using Diax.Domain.Common;

namespace Diax.Domain.AI;

public class AiProvider : AuditableEntity
{
    public string Key { get; private set; }
    public string Name { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool SupportsListModels { get; private set; }
    public string? BaseUrl { get; private set; }
    public bool IsVideoProvider { get; private set; }
    public bool IsTextProvider { get; private set; }

    // Navigation properties
    public ICollection<AiModel> Models { get; private set; } = new List<AiModel>();
    public AiProviderQuota? Quota { get; private set; }

    public AiProvider(string key, string name, bool supportsListModels, string? baseUrl = null)
    {
        Key = key;
        Name = name;
        IsEnabled = false; // Default disabled until configured
        SupportsListModels = supportsListModels;
        BaseUrl = baseUrl;
    }

    public void UpdateDetails(string name, bool supportsListModels, string? baseUrl)
    {
        Name = name;
        SupportsListModels = supportsListModels;
        BaseUrl = baseUrl;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
