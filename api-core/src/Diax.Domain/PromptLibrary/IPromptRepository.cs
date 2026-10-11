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

    /// <summary>Slugs iguais a <paramref name="baseSlug"/> ou com prefixo "<paramref name="baseSlug"/>-" (1 query).</summary>
    Task<IReadOnlyList<string>> GetSlugsLikeAsync(string baseSlug, CancellationToken ct = default);

    /// <summary>Prompts (com tags, tracking) que têm a tag — para rename em lote.</summary>
    Task<IReadOnlyList<Prompt>> GetForUpdateByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Descarta entidades pendentes no ChangeTracker depois de uma falha de SaveChanges,
    /// para que a próxima operação (ex.: próximo item de um import) não reenvie o lixo.
    /// </summary>
    void ResetTracking();
    Task<Prompt?> FindBySourceAsync(PromptSource source, Guid sourceRefId, CancellationToken ct = default);
    Task<IReadOnlyList<(string Name, int Count)>> GetTagCountsAsync(CancellationToken ct = default);
}

public interface IPromptCollectionRepository : IRepository<PromptCollection>
{
    Task<IReadOnlyList<(PromptCollection Collection, int PromptCount)>> GetAllWithCountsAsync(CancellationToken ct = default);
}
