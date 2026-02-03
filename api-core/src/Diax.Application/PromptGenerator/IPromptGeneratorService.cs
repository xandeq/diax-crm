using Diax.Application.PromptGenerator.Dtos;

namespace Diax.Application.PromptGenerator;

public interface IPromptGeneratorService
{
    Task<string> GenerateAsync(string rawPrompt, string provider, string promptType, string? model = null);

    Task<string> GenerateAndSaveAsync(
        string rawPrompt,
        string provider,
        string promptType,
        string? model,
        Guid userId);

    Task<IEnumerable<UserPromptHistoryDto>> GetUserHistoryAsync(
        Guid userId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<UserPromptDetailDto?> GetPromptByIdAsync(
        Guid promptId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
