using System.Text.Json;
using Diax.Application.Common;
using Diax.Application.PromptLibrary.Dtos;
using Diax.Domain.Common;
using Diax.Domain.PromptGenerator;
using Diax.Domain.PromptLibrary;
using Diax.Domain.Snippets;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Application.PromptLibrary;

public interface IPromptLibraryService
{
    Task<IReadOnlyList<PromptListItemDto>> ListAsync(PromptListQuery query, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    /// <summary>Detalhes (versão atual) de vários prompts em lote — usado pelo export.</summary>
    Task<IReadOnlyList<PromptDetailDto>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> CreateAsync(CreatePromptRequest request, Guid userId, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> UpdateAsync(Guid id, UpdatePromptRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> DuplicateAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result> RegisterUseAsync(Guid id, CancellationToken ct = default);
    Task<Result<RenderPromptResult>> RenderAsync(Guid id, RenderPromptRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PromptVersionSummaryDto>>> GetVersionsAsync(Guid id, CancellationToken ct = default);
    Task<Result<PromptVersionDto>> GetVersionAsync(Guid id, int number, CancellationToken ct = default);
    Task<Result<AddPromptVersionResult>> AddVersionAsync(Guid id, AddPromptVersionRequest request, CancellationToken ct = default);
    Task<Result<AddPromptVersionResult>> RestoreVersionAsync(Guid id, int number, CancellationToken ct = default);
    Task<IReadOnlyList<PromptTagCountDto>> GetTagsAsync(CancellationToken ct = default);
    Task<Result<int>> RenameTagAsync(RenameTagRequest request, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> CreateFromGeneratorAsync(Guid userPromptId, Guid userId, CancellationToken ct = default);
    Task<Result<PromptDetailDto>> CreateFromSnippetAsync(Guid snippetId, Guid userId, CancellationToken ct = default);
}

public class PromptLibraryService : IApplicationService, IPromptLibraryService
{
    public const string ErrNotFound = "Prompt.NotFound";
    public const string ErrValidation = "Prompt.Validation";
    public const string ErrConflict = "Prompt.Conflict";

    private static readonly HashSet<string> VariableTypes = new(StringComparer.Ordinal) { "text", "textarea", "select", "number" };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IPromptRepository _prompts;
    private readonly IPromptCollectionRepository _collections;
    private readonly IUserPromptRepository _generatorHistory;
    private readonly ISnippetRepository _snippets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PromptLibraryService> _logger;

    public PromptLibraryService(
        IPromptRepository prompts,
        IPromptCollectionRepository collections,
        IUserPromptRepository generatorHistory,
        ISnippetRepository snippets,
        IUnitOfWork unitOfWork,
        ILogger<PromptLibraryService> logger)
    {
        _prompts = prompts;
        _collections = collections;
        _generatorHistory = generatorHistory;
        _snippets = snippets;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ───────────────────────── Leitura ─────────────────────────

    public async Task<IReadOnlyList<PromptListItemDto>> ListAsync(PromptListQuery query, CancellationToken ct = default)
    {
        var q = _prompts.Query().Where(p => p.IsArchived == query.Archived);

        if (query.Favorite == true) q = q.Where(p => p.IsFavorite);
        if (query.ExposeToMcp.HasValue) q = q.Where(p => p.ExposeToMcp == query.ExposeToMcp.Value);
        if (query.CollectionId.HasValue) q = q.Where(p => p.CollectionId == query.CollectionId);
        if (TryParseKind(query.Kind, out var kind) && !string.IsNullOrWhiteSpace(query.Kind)) q = q.Where(p => p.Kind == kind);

        var tag = PromptTemplate.NormalizeTag(query.Tag);
        if (tag != null) q = q.Where(p => p.Tags.Any(t => t.Name == tag));

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(p =>
                p.Title.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)) ||
                p.Tags.Any(t => t.Name.Contains(term)) ||
                p.Versions.Any(v => v.Number == p.CurrentVersionNumber && v.Content.Contains(term)));
        }

        q = query.Sort switch
        {
            "used" => q.OrderByDescending(p => p.UseCount).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt),
            "title" => q.OrderBy(p => p.Title),
            "created" => q.OrderByDescending(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
        };

        var limit = Math.Clamp(query.Limit, 1, 5000); // padrão 2.000 (PromptListQuery); export pede 2.001 para detectar excesso
        var rows = await q.Take(limit).Select(ListProjection).ToListAsync(ct);
        return rows.Select(ToListItem).ToList();
    }

    public async Task<Result<PromptDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _prompts.Query().Where(p => p.Id == id).Select(DetailProjection).FirstOrDefaultAsync(ct);
        return row is null ? NotFound<PromptDetailDto>() : await BuildDetailAsync(row, ct);
    }

    public async Task<Result<PromptDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var s = PromptTemplate.Slugify(slug);
        var row = await _prompts.Query().Where(p => p.Slug == s).Select(DetailProjection).FirstOrDefaultAsync(ct);
        return row is null ? NotFound<PromptDetailDto>() : await BuildDetailAsync(row, ct);
    }

    public async Task<Result<IReadOnlyList<PromptVersionSummaryDto>>> GetVersionsAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _prompts.Query().AnyAsync(p => p.Id == id, ct)) return NotFound<IReadOnlyList<PromptVersionSummaryDto>>();
        var versions = await _prompts.GetVersionsAsync(id, ct);
        IReadOnlyList<PromptVersionSummaryDto> list = versions
            .Select(v => new PromptVersionSummaryDto(v.Number, v.ChangeNote, ToApi(v.Source), v.FrameworkApplied, v.Content.Length, v.CreatedAt))
            .ToList();
        return Result.Success(list);
    }

    public async Task<Result<PromptVersionDto>> GetVersionAsync(Guid id, int number, CancellationToken ct = default)
    {
        var v = await _prompts.GetVersionAsync(id, number, ct);
        return v is null
            ? Result.Failure<PromptVersionDto>(new Error(ErrNotFound, $"Versão v{number} não encontrada."))
            : Result.Success(ToVersionDto(v));
    }

    public async Task<IReadOnlyList<PromptTagCountDto>> GetTagsAsync(CancellationToken ct = default)
        => (await _prompts.GetTagCountsAsync(ct)).Select(t => new PromptTagCountDto(t.Name, t.Count)).ToList();

    public async Task<Result<RenderPromptResult>> RenderAsync(Guid id, RenderPromptRequest request, CancellationToken ct = default)
    {
        var current = await _prompts.Query().Where(p => p.Id == id).Select(p => (int?)p.CurrentVersionNumber).FirstOrDefaultAsync(ct);
        if (current is null) return NotFound<RenderPromptResult>();

        var number = request.VersionNumber ?? current.Value;
        var version = await _prompts.GetVersionAsync(id, number, ct);
        if (version is null) return Result.Failure<RenderPromptResult>(new Error(ErrNotFound, $"Versão v{number} não encontrada."));

        var variables = ResolveVariables(version.Content, DeserializeVariables(version.VariablesJson));
        var defaults = variables.ToDictionary(v => v.Name, v => v.Default, StringComparer.Ordinal);
        var optional = variables.Where(v => !v.Required).Select(v => v.Name).ToList();
        var result = PromptTemplate.Render(version.Content, request.Variables, defaults, optional);

        if (request.Strict && result.Missing.Count > 0)
            return Result.Failure<RenderPromptResult>(new Error(ErrValidation, "Variáveis obrigatórias sem valor: " + string.Join(", ", result.Missing)));

        return Result.Success(new RenderPromptResult(result.Rendered, result.Missing, result.Unused, variables, number));
    }

    // ───────────────────────── Escrita ─────────────────────────

    public Task<Result<PromptDetailDto>> CreateAsync(CreatePromptRequest request, Guid userId, CancellationToken ct = default)
        => CreateWithSourceAsync(request, userId, ParseSource(request.Source), null, ct);

    public async Task<IReadOnlyList<PromptDetailDto>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        var result = new List<PromptDetailDto>(ids.Count);
        // Lotes de 500: fica longe do limite de 2100 parâmetros do SQL Server.
        foreach (var chunk in ids.Distinct().Chunk(500))
        {
            var rows = await _prompts.Query().Where(p => chunk.Contains(p.Id)).Select(ListProjection).ToListAsync(ct);
            var versions = await _prompts.GetCurrentVersionsAsync(chunk, ct);
            result.AddRange(rows
                .Where(r => versions.ContainsKey(r.Id))
                .Select(r => new PromptDetailDto(ToListItem(r), r.TargetModel, ToVersionDto(versions[r.Id]))));
        }
        return result.OrderBy(d => d.Prompt.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<Result<PromptDetailDto>> UpdateAsync(Guid id, UpdatePromptRequest request, CancellationToken ct = default)
    {
        var prompt = await _prompts.GetForUpdateAsync(id, ct);
        if (prompt is null) return NotFound<PromptDetailDto>();

        try
        {
            if (request.Title != null) prompt.Rename(request.Title);
            if (request.Slug != null)
            {
                var slug = PromptTemplate.Slugify(request.Slug);
                if (slug != prompt.Slug)
                {
                    if (await _prompts.SlugExistsAsync(slug, prompt.Id, ct))
                        return Result.Failure<PromptDetailDto>(new Error(ErrConflict, $"Já existe um prompt com o slug '{slug}'."));
                    prompt.ChangeSlug(slug);
                }
            }
            if (request.Description != null) prompt.SetDescription(request.Description);
            if (request.Tags != null) prompt.SetTags(request.Tags);
            if (request.ClearCollection) prompt.MoveToCollection(null);
            else if (request.CollectionId.HasValue)
            {
                if (!await CollectionExistsAsync(request.CollectionId.Value, ct))
                    return Result.Failure<PromptDetailDto>(new Error(ErrValidation, "Coleção não encontrada."));
                prompt.MoveToCollection(request.CollectionId);
            }
            if (request.Kind != null || request.Framework != null || request.TargetModel != null)
            {
                var kind = prompt.Kind;
                if (request.Kind != null && !TryParseKind(request.Kind, out kind))
                    return Result.Failure<PromptDetailDto>(new Error(ErrValidation, $"Tipo inválido: {request.Kind}."));
                prompt.SetClassification(kind, request.Framework ?? prompt.Framework, request.TargetModel ?? prompt.TargetModel);
            }
            if (request.IsFavorite.HasValue) prompt.SetFavorite(request.IsFavorite.Value);
            if (request.IsArchived.HasValue) prompt.SetArchived(request.IsArchived.Value);
            if (request.ExposeToMcp.HasValue) prompt.SetExposeToMcp(request.ExposeToMcp.Value);

            await _unitOfWork.SaveChangesAsync(ct);
            return await GetAsync(id, ct);
        }
        catch (ArgumentException ex)
        {
            _prompts.ResetTracking();
            return Result.Failure<PromptDetailDto>(new Error(ErrValidation, ex.Message));
        }
        catch (DbUpdateException ex)
        {
            _prompts.ResetTracking();
            _logger.LogWarning(ex, "Falha ao atualizar prompt {PromptId}", id);
            return Result.Failure<PromptDetailDto>(new Error(ErrConflict, "Não foi possível salvar (o slug pode ter sido usado ao mesmo tempo). Recarregue e tente de novo."));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var prompt = await _prompts.GetForUpdateAsync(id, ct);
        if (prompt is null) return Result.Failure(new Error(ErrNotFound, "Prompt não encontrado."));
        await _prompts.DeleteAsync(prompt, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<PromptDetailDto>> DuplicateAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var source = await GetAsync(id, ct);
        if (source.IsFailure) return source;
        var d = source.Value!;
        var request = new CreatePromptRequest
        {
            Title = Truncate($"{d.Prompt.Title} (cópia)", Prompt.MaxTitleLength),
            Description = d.Prompt.Description,
            Content = d.Current.Content,
            Variables = d.Current.Variables.ToList(),
            Tags = d.Prompt.Tags.ToList(),
            CollectionId = d.Prompt.CollectionId,
            Kind = d.Prompt.Kind,
            Framework = d.Prompt.Framework,
            TargetModel = d.TargetModel,
            ChangeNote = $"Duplicado de {d.Prompt.Slug} v{d.Current.Number}"
        };
        return await CreateAsync(request, userId, ct);
    }

    public async Task<Result> RegisterUseAsync(Guid id, CancellationToken ct = default)
    {
        var prompt = await _prompts.GetByIdAsync(id, ct);
        if (prompt is null) return Result.Failure(new Error(ErrNotFound, "Prompt não encontrado."));
        prompt.RegisterUse();
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AddPromptVersionResult>> AddVersionAsync(Guid id, AddPromptVersionRequest request, CancellationToken ct = default)
    {
        var prompt = await _prompts.GetForUpdateAsync(id, ct);
        if (prompt is null) return NotFound<AddPromptVersionResult>();

        try
        {
            var variablesJson = SerializeVariables(ResolveVariables(request.Content, request.Variables));
            var version = prompt.AddVersion(request.Content, variablesJson, request.ChangeNote, ParseSource(request.Source), request.BaseVersionNumber, request.FrameworkApplied);
            if (version is null)
            {
                var current = await _prompts.GetVersionAsync(id, prompt.CurrentVersionNumber, ct);
                return Result.Success(new AddPromptVersionResult(ToVersionDto(current!), false));
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("prompt.versioned {PromptId} v{Number}", id, version.Number);
            return Result.Success(new AddPromptVersionResult(ToVersionDto(version), true));
        }
        catch (PromptVersionConflictException ex)
        {
            _prompts.ResetTracking();
            return Result.Failure<AddPromptVersionResult>(new Error(ErrConflict, ex.Message));
        }
        catch (ArgumentException ex)
        {
            _prompts.ResetTracking();
            return Result.Failure<AddPromptVersionResult>(new Error(ErrValidation, ex.Message));
        }
        catch (DbUpdateException ex)
        {
            // Com os tamanhos validados no domínio, a falha restante é o índice único
            // (prompt_id, number): outra gravação venceu a corrida.
            _prompts.ResetTracking();
            _logger.LogWarning(ex, "Conflito de versão ao salvar prompt {PromptId}", id);
            return Result.Failure<AddPromptVersionResult>(new Error(ErrConflict, "O prompt foi alterado ao mesmo tempo em outro lugar. Recarregue e tente de novo."));
        }
    }

    public async Task<Result<AddPromptVersionResult>> RestoreVersionAsync(Guid id, int number, CancellationToken ct = default)
    {
        var current = await _prompts.Query().Where(p => p.Id == id).Select(p => (int?)p.CurrentVersionNumber).FirstOrDefaultAsync(ct);
        if (current is null) return NotFound<AddPromptVersionResult>();
        var old = await _prompts.GetVersionAsync(id, number, ct);
        if (old is null) return Result.Failure<AddPromptVersionResult>(new Error(ErrNotFound, $"Versão v{number} não encontrada."));

        return await AddVersionAsync(id, new AddPromptVersionRequest
        {
            Content = old.Content,
            Variables = DeserializeVariables(old.VariablesJson)?.ToList(),
            ChangeNote = $"Restaurado da v{number}",
            BaseVersionNumber = current.Value,
            Source = "manual"
        }, ct);
    }

    public async Task<Result<int>> RenameTagAsync(RenameTagRequest request, CancellationToken ct = default)
    {
        var from = PromptTemplate.NormalizeTag(request.From);
        var to = PromptTemplate.NormalizeTag(request.To);
        if (from is null || to is null) return Result.Failure<int>(new Error(ErrValidation, "Informe a tag de origem e a de destino."));
        if (from == to) return Result.Success(0);

        var prompts = await _prompts.GetForUpdateByTagAsync(from, ct);
        foreach (var p in prompts)
            p.SetTags(p.Tags.Select(t => t.Name == from ? to : t.Name).ToList());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success(prompts.Count);
    }

    // ───────────────────── Origens (migração opt-in) ─────────────────────

    public async Task<Result<PromptDetailDto>> CreateFromGeneratorAsync(Guid userPromptId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _prompts.FindBySourceAsync(PromptSource.Generator, userPromptId, ct);
        if (existing != null) return await GetAsync(existing.Id, ct);

        var item = await _generatorHistory.GetByIdWithUserAsync(userPromptId, userId, ct);
        if (item is null) return Result.Failure<PromptDetailDto>(new Error(ErrNotFound, "Item do histórico do Gerador não encontrado."));

        var request = new CreatePromptRequest
        {
            Title = Truncate(FirstLine(item.OriginalInput), Prompt.MaxTitleLength),
            Description = Truncate(item.GetInputPreview(Prompt.MaxDescriptionLength - 3), Prompt.MaxDescriptionLength),
            Content = item.GeneratedPrompt,
            // As colunas do histórico do Gerador são maiores (50/100): ajusta em vez de recusar.
            Framework = TruncateOrNull(item.PromptType, Prompt.MaxFrameworkLength),
            TargetModel = TruncateOrNull(item.Model, Prompt.MaxTargetModelLength),
            ChangeNote = $"Gerado com {item.PromptType} via {item.Provider}"
        };
        return await CreateWithSourceAsync(request, userId, PromptSource.Generator, userPromptId, ct);
    }

    public async Task<Result<PromptDetailDto>> CreateFromSnippetAsync(Guid snippetId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _prompts.FindBySourceAsync(PromptSource.Snippet, snippetId, ct);
        if (existing != null) return await GetAsync(existing.Id, ct);

        var snippet = await _snippets.GetByIdWithUserAsync(snippetId, userId, ct);
        if (snippet is null) return Result.Failure<PromptDetailDto>(new Error(ErrNotFound, "Snippet não encontrado."));
        if (snippet.Language is not ("text" or "markdown"))
            return Result.Failure<PromptDetailDto>(new Error(ErrValidation, "Só snippets de texto ou markdown podem virar prompt."));
        if (string.IsNullOrWhiteSpace(snippet.Content))
            return Result.Failure<PromptDetailDto>(new Error(ErrValidation, "O snippet não tem conteúdo de texto."));

        var request = new CreatePromptRequest
        {
            Title = Truncate(snippet.Title, Prompt.MaxTitleLength),
            Content = snippet.Content,
            ChangeNote = "Convertido de Snippet"
        };
        return await CreateWithSourceAsync(request, userId, PromptSource.Snippet, snippetId, ct);
    }

    private async Task<Result<PromptDetailDto>> CreateWithSourceAsync(CreatePromptRequest request, Guid userId, PromptSource source, Guid? sourceRefId, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var prompt = await NewPromptAsync(userId, request, source, sourceRefId, ct);
                await _prompts.AddAsync(prompt, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("prompt.created {PromptId} source={Source}", prompt.Id, source);
                return await GetAsync(prompt.Id, ct);
            }
            catch (ArgumentException ex)
            {
                _prompts.ResetTracking();
                return Result.Failure<PromptDetailDto>(new Error(ErrValidation, ex.Message));
            }
            catch (DbUpdateException ex)
            {
                // Corrida num índice único: mesma origem (ex.: clique duplo no Gerador) → devolve o
                // existente (idempotência real); slug → tenta de novo com outro sufixo.
                _prompts.ResetTracking();
                if (sourceRefId is { } refId && await _prompts.FindBySourceAsync(source, refId, ct) is { } existing)
                    return await GetAsync(existing.Id, ct);
                if (attempt >= 2)
                {
                    _logger.LogWarning(ex, "Falha ao criar prompt depois de {Attempts} tentativas", attempt);
                    return Result.Failure<PromptDetailDto>(new Error(ErrConflict, "Não foi possível salvar o prompt agora (conflito de slug). Tente de novo."));
                }
            }
        }
    }

    // ───────────────────────── Helpers ─────────────────────────

    private async Task<Prompt> NewPromptAsync(Guid userId, CreatePromptRequest request, PromptSource source, Guid? sourceRefId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("Título é obrigatório.");
        if (!TryParseKind(request.Kind, out var kind)) throw new ArgumentException($"Tipo inválido: {request.Kind}.");
        if (request.CollectionId.HasValue && !await CollectionExistsAsync(request.CollectionId.Value, ct))
            throw new ArgumentException("Coleção não encontrada.");

        var slug = await UniqueSlugAsync(string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug, ct);
        var variablesJson = SerializeVariables(ResolveVariables(request.Content, request.Variables));

        var prompt = Prompt.Create(userId, request.Title, slug, request.Content, variablesJson, request.ChangeNote ?? "Versão inicial", source, sourceRefId);
        prompt.SetDescription(request.Description);
        prompt.SetClassification(kind, request.Framework, request.TargetModel);
        prompt.MoveToCollection(request.CollectionId);
        prompt.SetTags(request.Tags);
        prompt.SetFavorite(request.IsFavorite);
        prompt.SetExposeToMcp(request.ExposeToMcp);
        return prompt;
    }

    private async Task<bool> CollectionExistsAsync(Guid id, CancellationToken ct)
        => await _collections.ExistsAsync(id, ct);

    private async Task<string> UniqueSlugAsync(string text, CancellationToken ct)
    {
        var baseSlug = PromptTemplate.Slugify(text);
        // 1 query traz base e base-N; o sufixo é escolhido em memória.
        var taken = new HashSet<string>(await _prompts.GetSlugsLikeAsync(baseSlug, ct), StringComparer.Ordinal);
        if (!taken.Contains(baseSlug)) return baseSlug;
        for (var i = 2; i < 10_000; i++)
        {
            var suffix = "-" + i;
            var truncated = baseSlug.Length + suffix.Length > PromptTemplate.MaxSlugLength;
            var candidate = (truncated ? baseSlug[..(PromptTemplate.MaxSlugLength - suffix.Length)].TrimEnd('-') : baseSlug) + suffix;
            if (taken.Contains(candidate)) continue;
            // Slug truncado não tem o prefixo da query: confirma no banco.
            if (truncated && await _prompts.SlugExistsAsync(candidate, null, ct)) continue;
            return candidate;
        }
        var fallback = $"{baseSlug[..Math.Min(baseSlug.Length, 40)]}-{Guid.NewGuid():N}";
        return fallback[..Math.Min(fallback.Length, PromptTemplate.MaxSlugLength)];
    }

    /// <summary>
    /// Variáveis persistidas = as usadas no conteúdo, na ordem de aparição. Metadados declarados são
    /// mantidos; as não declaradas viram <c>text</c> obrigatória; declaradas e não usadas são descartadas.
    /// </summary>
    internal static IReadOnlyList<PromptVariableDto> ResolveVariables(string content, IEnumerable<PromptVariableDto>? declared)
    {
        var byName = (declared ?? Enumerable.Empty<PromptVariableDto>())
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .GroupBy(v => v.Name.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        return PromptTemplate.ExtractVariables(content).Select(name =>
        {
            if (!byName.TryGetValue(name, out var d)) return new PromptVariableDto(name);
            var type = VariableTypes.Contains(d.Type ?? "") ? d.Type! : "text";
            var options = type == "select" ? d.Options?.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).Distinct().ToList() : null;
            return new PromptVariableDto(
                name,
                string.IsNullOrWhiteSpace(d.Label) ? null : d.Label.Trim(),
                type,
                string.IsNullOrEmpty(d.Default) ? null : d.Default,
                d.Required,
                options);
        }).ToList();
    }

    private static string? SerializeVariables(IReadOnlyList<PromptVariableDto> vars)
        => vars.Count == 0 ? null : JsonSerializer.Serialize(vars, Json);

    private static IReadOnlyList<PromptVariableDto>? DeserializeVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<PromptVariableDto>>(json, Json); }
        catch (JsonException) { return null; }
    }

    private static PromptVersionDto ToVersionDto(PromptVersion v) => new(
        v.Number,
        v.Content,
        ResolveVariables(v.Content, DeserializeVariables(v.VariablesJson)),
        v.ChangeNote,
        ToApi(v.Source),
        v.FrameworkApplied,
        v.CreatedAt);

    private sealed record ListRow(
        Guid Id, string Slug, string Title, string? Description, PromptKind Kind, string? Framework, Guid? CollectionId,
        List<string> Tags, string Preview, string? VariableNames, bool IsFavorite, bool IsArchived, bool ExposeToMcp,
        int CurrentVersionNumber, int UseCount, DateTime? LastUsedAt, DateTime CreatedAt, DateTime? UpdatedAt, PromptSource Source,
        string? TargetModel);

    private static readonly System.Linq.Expressions.Expression<Func<Prompt, ListRow>> ListProjection = p => new ListRow(
        p.Id, p.Slug, p.Title, p.Description, p.Kind, p.Framework, p.CollectionId,
        p.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToList(),
        p.Preview, p.VariableNames, p.IsFavorite, p.IsArchived, p.ExposeToMcp,
        p.CurrentVersionNumber, p.UseCount, p.LastUsedAt, p.CreatedAt, p.UpdatedAt, p.Source, p.TargetModel);

    private static readonly System.Linq.Expressions.Expression<Func<Prompt, ListRow>> DetailProjection = ListProjection;

    private static PromptListItemDto ToListItem(ListRow r) => new(
        r.Id, r.Slug, r.Title, r.Description, ToApi(r.Kind), r.Framework, r.CollectionId, r.Tags, r.Preview,
        string.IsNullOrEmpty(r.VariableNames) ? Array.Empty<string>() : r.VariableNames.Split(',', StringSplitOptions.RemoveEmptyEntries),
        r.IsFavorite, r.IsArchived, r.ExposeToMcp, r.CurrentVersionNumber, r.UseCount, r.LastUsedAt,
        r.CreatedAt, r.UpdatedAt ?? r.CreatedAt, ToApi(r.Source));

    private async Task<Result<PromptDetailDto>> BuildDetailAsync(ListRow row, CancellationToken ct)
    {
        var current = await _prompts.GetVersionAsync(row.Id, row.CurrentVersionNumber, ct);
        if (current is null) return Result.Failure<PromptDetailDto>(new Error(ErrNotFound, "Versão atual não encontrada."));
        return Result.Success(new PromptDetailDto(ToListItem(row), row.TargetModel, ToVersionDto(current)));
    }

    internal static bool TryParseKind(string? value, out PromptKind kind)
    {
        kind = PromptKind.Prompt;
        if (string.IsNullOrWhiteSpace(value)) return true;
        return Enum.TryParse(value.Trim(), ignoreCase: true, out kind) && Enum.IsDefined(kind);
    }

    internal static PromptSource ParseSource(string? value)
    {
        // Fontes aceitas pela API pública; "git" fica reservado para o sync (V2).
        if (string.IsNullOrWhiteSpace(value)) return PromptSource.Manual;
        return Enum.TryParse<PromptSource>(value.Trim(), ignoreCase: true, out var s) && s != PromptSource.Git ? s : PromptSource.Manual;
    }

    private static string ToApi(PromptKind k) => k.ToString().ToLowerInvariant();
    private static string ToApi(PromptSource s) => s.ToString().ToLowerInvariant();

    private static Result<T> NotFound<T>() => Result.Failure<T>(new Error(ErrNotFound, "Prompt não encontrado."));

    private static string FirstLine(string text)
    {
        var line = (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(line) ? "Prompt do Gerador" : line;
    }

    private static string? TruncateOrNull(string? text, int max)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim().Length <= max ? text.Trim() : text.Trim()[..max];

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
}
