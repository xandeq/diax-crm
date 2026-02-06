namespace Diax.Application.AI.Dtos;

/// <summary>
/// Represents a model discovered from an AI provider's API
/// </summary>
public record DiscoveredModelDto(
    string Id,
    string Name,
    string Provider,
    int? ContextLength = null,
    string? InputCostHint = null,
    string? OutputCostHint = null
);
