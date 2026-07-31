using Diax.Domain.Common;

namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Representa um ativo do patrimônio do usuário (ouro, ações, imóveis, milhas, etc).
/// Agregado raiz do módulo Patrimônio &amp; Investimentos; possui histórico de avaliações.
/// </summary>
public class Asset : AuditableEntity, IUserOwnedEntity
{
    public const string DefaultCurrency = "BRL";

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AssetClass Class { get; private set; }
    public AssetOwnership Ownership { get; private set; }
    public AssetLiquidity Liquidity { get; private set; }
    public string Currency { get; private set; } = DefaultCurrency;
    public decimal CurrentValue { get; private set; }
    public decimal? CostBasis { get; private set; }
    public DateTime? AcquiredAt { get; private set; }
    public AssetValuationSource ValuationSource { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual ICollection<AssetValuation> Valuations { get; private set; } = new List<AssetValuation>();

    private Asset() { }

    public static Asset Create(
        Guid userId,
        string name,
        AssetClass assetClass,
        AssetOwnership ownership,
        AssetLiquidity liquidity,
        decimal currentValue,
        AssetValuationSource valuationSource,
        string? currency = null,
        decimal? costBasis = null,
        DateTime? acquiredAt = null,
        string? notes = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        ValidateName(name);
        ValidateValue(currentValue);

        return new Asset
        {
            UserId = userId,
            Name = name,
            Class = assetClass,
            Ownership = ownership,
            Liquidity = liquidity,
            Currency = NormalizeCurrency(currency),
            CurrentValue = currentValue,
            CostBasis = costBasis,
            AcquiredAt = acquiredAt,
            ValuationSource = valuationSource,
            Notes = notes
        };
    }

    /// <summary>
    /// Atualiza somente o valor atual do ativo.
    /// </summary>
    public void UpdateValue(decimal newValue)
    {
        ValidateValue(newValue);

        CurrentValue = newValue;
        SetUpdated();
    }

    /// <summary>
    /// Atualiza os dados cadastrais do ativo.
    /// </summary>
    public void UpdateDetails(
        string name,
        AssetClass assetClass,
        AssetOwnership ownership,
        AssetLiquidity liquidity,
        decimal currentValue,
        AssetValuationSource valuationSource,
        string? currency = null,
        decimal? costBasis = null,
        DateTime? acquiredAt = null,
        string? notes = null)
    {
        ValidateName(name);
        ValidateValue(currentValue);

        Name = name;
        Class = assetClass;
        Ownership = ownership;
        Liquidity = liquidity;
        Currency = NormalizeCurrency(currency);
        CurrentValue = currentValue;
        CostBasis = costBasis;
        AcquiredAt = acquiredAt;
        ValuationSource = valuationSource;
        Notes = notes;
        SetUpdated();
    }

    /// <summary>
    /// Registra uma nova avaliação no histórico e atualiza o valor atual do ativo.
    /// </summary>
    public AssetValuation AddValuation(decimal value, DateTime asOf, AssetValuationSource source, string? currency = null)
    {
        var valuation = new AssetValuation(
            Id,
            value,
            string.IsNullOrWhiteSpace(currency) ? Currency : NormalizeCurrency(currency),
            asOf,
            source);

        Valuations.Add(valuation);
        UpdateValue(value);

        return valuation;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Asset name cannot exceed 200 characters", nameof(name));
    }

    private static void ValidateValue(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Asset value cannot be negative", nameof(value));
    }

    private static string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? DefaultCurrency
            : currency.Trim().ToUpperInvariant();
    }
}
