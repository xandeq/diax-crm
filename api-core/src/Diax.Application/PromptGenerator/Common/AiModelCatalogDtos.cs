namespace Diax.Application.PromptGenerator.Common;

public record AiModelDto(string Id, string Name, string Category, bool IsDefault = false);

public record ProviderModelsDto(string ProviderId, string ProviderName, List<AiModelDto> Models);
