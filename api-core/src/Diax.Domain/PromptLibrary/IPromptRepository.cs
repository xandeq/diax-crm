using Diax.Domain.Common;

namespace Diax.Domain.PromptLibrary;

public interface IPromptRepository : IRepository<Prompt>
{
    /// <summary>Consulta com o filtro global de usuário aplicado (sem tracking).</summary>
    IQueryable<Prompt> Query();

    /// <summary>Carrega o prompt com tags (tracking) para edição.</summary>
    Task<Prompt?> GetForUpdateAsync(Guid id, CancellationToken ct = default);

    Task<PromptVersion?> GetVersionAsync(Guid promptId, int number, CancellationToken ct = default);
    Task<IReadOnlyList<PromptVersion>> GetVersionsAsync(Guid promptId, CancellationToken ct = default);

    /// <summary>Versões atuais (conteúdo) de vários prompts, para export/busca.</summary>
    Task<Dictionary<Guid, PromptVersion>> GetCurrentVersionsAsync(IEnumerable<Guid> promptIds, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct = default);
    Task<Prompt?> FindBySourceAsync(PromptSource source, Guid sourceRefId, CancellationToken ct = default);
    Task<IReadOnlyList<(string Name, int Count)>> GetTagCountsAsync(CancellationToken ct = default);
}

public interface IPromptCollectionRepository : IRepository<PromptCollection>
{
    Task<IReadOnlyList<(PromptCollection Collection, int PromptCount)>> GetAllWithCountsAsync(CancellationToken ct = default);
}
