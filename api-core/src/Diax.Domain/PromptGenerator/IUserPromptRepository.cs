using Diax.Domain.Common;

namespace Diax.Domain.PromptGenerator;

/// <summary>
/// Interface do repositório para a entidade UserPrompt.
/// </summary>
public interface IUserPromptRepository : IRepository<UserPrompt>
{
    /// <summary>
    /// Obtém o histórico de prompts de um usuário específico, ordenado pelo mais recente.
    /// </summary>
    Task<IEnumerable<UserPrompt>> GetByUserIdAsync(
        Guid userId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um prompt específico garantindo que pertença ao usuário informado.
    /// </summary>
    Task<UserPrompt?> GetByIdWithUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}
