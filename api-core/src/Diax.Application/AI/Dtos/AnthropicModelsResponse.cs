namespace Diax.Application.AI.Dtos;

public record AnthropicModelsResponse(
    List<AnthropicModel> Data,
    bool HasMore,
    string? FirstId,
    string? LastId
);

public record AnthropicModel(
    string Id,
    string DisplayName,
    DateTime CreatedAt,
    string Type
);
