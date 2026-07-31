using Diax.Domain.Common;

namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Snapshot histórico do valor de um ativo em uma data (AsOf).
/// </summary>
public class AssetValuation : Entity
{
    public Guid AssetId { get; private set; }
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = Asset.DefaultCurrency;
    public DateTime AsOf { get; private set; }
    public AssetValuationSource Source { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public virtual Asset Asset { get; private set; } = null!;

    private AssetValuation() { }

    public AssetValuation(
        Guid assetId,
        decimal value,
        string currency,
        DateTime asOf,
        AssetValuationSource source)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required", nameof(assetId));

        if (value < 0)
            throw new ArgumentException("Valuation value cannot be negative", nameof(value));

        AssetId = assetId;
        Value = value;
        Currency = string.IsNullOrWhiteSpace(currency) ? Asset.DefaultCurrency : currency.Trim().ToUpperInvariant();
        AsOf = asOf;
        Source = source;
        CreatedAt = DateTime.UtcNow;
    }
}
