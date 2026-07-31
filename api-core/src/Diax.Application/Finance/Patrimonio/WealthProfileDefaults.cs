using System.Text.Json;
using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Defaults do perfil patrimonial (F2) + helpers de (de)serialização da alocação-alvo.
/// Alocação-alvo default ordenada por facilidade de execução (ease-ranked).
/// </summary>
public static class WealthProfileDefaults
{
    public const string RiskProfile = WealthProfile.DefaultRiskProfile; // "builder_all"
    public const decimal GoalAmount = 1_000_000m;
    public const int GoalYears = 5;

    /// <summary>
    /// Alocação-alvo default por classe (%). Soma 100.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, decimal> TargetAllocation =
        new Dictionary<string, decimal>
        {
            [nameof(AssetClass.RendaFixa)] = 25m,
            [nameof(AssetClass.Acao)] = 20m,
            [nameof(AssetClass.Etf)] = 15m,
            [nameof(AssetClass.Fii)] = 15m,
            [nameof(AssetClass.Exterior)] = 10m,
            [nameof(AssetClass.Ouro)] = 5m,
            [nameof(AssetClass.Cripto)] = 5m,
            [nameof(AssetClass.Moeda)] = 5m
        };

    public static string TargetAllocationJson() => SerializeAllocation(TargetAllocation);

    public static string SerializeAllocation(IReadOnlyDictionary<string, decimal> allocation) =>
        JsonSerializer.Serialize(allocation);

    /// <summary>
    /// Desserializa o JSON de alocação em chaves string (nomes de AssetClass).
    /// Retorna os defaults se o JSON for nulo/inválido.
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> ParseAllocation(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return TargetAllocation;

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json);
            return parsed is { Count: > 0 } ? parsed : TargetAllocation;
        }
        catch (JsonException)
        {
            return TargetAllocation;
        }
    }

    /// <summary>
    /// Converte a alocação (chaves string) para chaves AssetClass, ignorando chaves desconhecidas.
    /// </summary>
    public static Dictionary<AssetClass, decimal> ToClassAllocation(IReadOnlyDictionary<string, decimal> allocation)
    {
        var result = new Dictionary<AssetClass, decimal>();
        foreach (var (key, pct) in allocation)
        {
            if (Enum.TryParse<AssetClass>(key, ignoreCase: true, out var assetClass))
            {
                result[assetClass] = result.TryGetValue(assetClass, out var existing) ? existing + pct : pct;
            }
        }

        return result;
    }
}
