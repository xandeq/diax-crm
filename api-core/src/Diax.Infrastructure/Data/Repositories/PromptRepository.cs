using Diax.Domain.PromptLibrary;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class PromptRepository : Repository<Prompt>, IPromptRepository
{
    public PromptRepository(DiaxDbContext context) : base(context) { }

    public IQueryable<Prompt> Query() => DbSet.AsNoTracking();

    public Task<Prompt?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        => DbSet.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id, ct);

    // Versões não têm filtro global: o acesso passa sempre por um prompt do usuário.
    public Task<PromptVersion?> GetVersionAsync(Guid promptId, int number, CancellationToken ct = default)
        => Context.Set<PromptVersion>().AsNoTracking()
            .Where(v => v.PromptId == promptId && v.Number == number
                        && DbSet.Any(p => p.Id == promptId))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PromptVersion>> GetVersionsAsync(Guid promptId, CancellationToken ct = default)
        => await Context.Set<PromptVersion>().AsNoTracking()
            .Where(v => v.PromptId == promptId && DbSet.Any(p => p.Id == promptId))
            .OrderByDescending(v => v.Number)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, PromptVersion>> GetCurrentVersionsAsync(IEnumerable<Guid> promptIds, CancellationToken ct = default)
    {
        var ids = promptIds.Distinct().ToList();
        if (ids.Count == 0) return new();

        var versions = await (
            from p in DbSet.AsNoTracking()
            join v in Context.Set<PromptVersion>().AsNoTracking()
                on new { PromptId = p.Id, Number = p.CurrentVersionNumber } equals new { v.PromptId, v.Number }
            where ids.Contains(p.Id)
            select v).ToListAsync(ct);

        return versions.ToDictionary(v => v.PromptId);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct = default)
        => DbSet.AnyAsync(p => p.Slug == slug && (exceptId == null || p.Id != exceptId), ct);

    public async Task<IReadOnlyList<string>> GetSlugsLikeAsync(string baseSlug, CancellationToken ct = default)
    {
        var prefix = baseSlug + "-";
        return await DbSet.AsNoTracking()
            .Where(p => p.Slug == baseSlug || p.Slug.StartsWith(prefix))
            .Select(p => p.Slug)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Prompt>> GetForUpdateByTagAsync(string tag, CancellationToken ct = default)
        => await DbSet.Include(p => p.Tags).Where(p => p.Tags.Any(t => t.Name == tag)).ToListAsync(ct);

    public void ResetTracking() => Context.ChangeTracker.Clear();

    public Task<Prompt?> FindBySourceAsync(PromptSource source, Guid sourceRefId, CancellationToken ct = default)
        => DbSet.AsNoTracking().FirstOrDefaultAsync(p => p.Source == source && p.SourceRefId == sourceRefId, ct);

    public async Task<IReadOnlyList<(string Name, int Count)>> GetTagCountsAsync(CancellationToken ct = default)
    {
        var rows = await DbSet.AsNoTracking()
            .Where(p => !p.IsArchived)
            .SelectMany(p => p.Tags)
            .GroupBy(t => t.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(r => (r.Name, r.Count)).ToList();
    }
}

public class PromptCollectionRepository : Repository<PromptCollection>, IPromptCollectionRepository
{
    public PromptCollectionRepository(DiaxDbContext context) : base(context) { }

    public async Task<IReadOnlyList<(PromptCollection Collection, int PromptCount)>> GetAllWithCountsAsync(CancellationToken ct = default)
    {
        var rows = await DbSet.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new
            {
                Collection = c,
                Count = Context.Set<Prompt>().Count(p => p.CollectionId == c.Id && !p.IsArchived)
            })
            .ToListAsync(ct);
        return rows.Select(r => (r.Collection, r.Count)).ToList();
    }

    public void ResetTracking() => Context.ChangeTracker.Clear();
}
