namespace Diax.Application.Finance.Patrimonio.Dtos;

/// <summary>
/// Atualização do perfil patrimonial. Campos nulos mantêm o valor atual.
/// TargetAllocation: percentual-alvo por classe (chave = nome do AssetClass, ex: "RendaFixa").
/// </summary>
public record UpdateWealthProfileRequest(
    string? RiskProfile = null,
    decimal? GoalAmount = null,
    int? GoalYears = null,
    Dictionary<string, decimal>? TargetAllocation = null
);
