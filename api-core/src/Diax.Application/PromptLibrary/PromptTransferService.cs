using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Diax.Application.Common;
using Diax.Application.PromptLibrary.Dtos;
using Diax.Domain.PromptLibrary;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.PromptLibrary;

public interface IPromptTransferService
{
    Task<Result<ExportFile>> ExportAsync(IReadOnlyCollection<Guid>? ids, string format, string flavor, CancellationToken ct = default);
    Task<Result<ImportReportDto>> ImportAsync(IReadOnlyList<ImportUpload> files, bool dryRun, string strategy, Guid userId, CancellationToken ct = default);
}

public sealed record ExportFile(string FileName, string ContentType, byte[] Content);
public sealed record ImportUpload(string FileName, byte[] Content);

public sealed record ImportItemDto(
    string File,
    string Action,          // create | newVersion | unchanged | skip | error
    string? Slug,
    string? Title,
    Guid? PromptId,
    IReadOnlyList<string> Warnings,
    string? Error);

public sealed record ImportSummaryDto(int Create, int NewVersion, int Unchanged, int Skip, int Errors);
public sealed record ImportReportDto(bool DryRun, string Strategy, ImportSummaryDto Summary, IReadOnlyList<ImportItemDto> Items);

/// <summary>Import/export no formato canônico .md (ver <see cref="PromptMarkdownSerializer"/>).</summary>
public class PromptTransferService : IApplicationService, IPromptTransferService
{
    public const long MaxFileBytes = 512 * 1024;
    public const long MaxZipBytes = 5 * 1024 * 1024;
    public const int MaxZipEntries = 500;
    public const long MaxUncompressedBytes = 20 * 1024 * 1024;
    public const int MaxExportPrompts = 2000;
    public static readonly string[] Strategies = { "new-version", "skip", "duplicate" };

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IPromptLibraryService _library;
    private readonly IPromptCollectionService _collections;
    private readonly ILogger<PromptTransferService> _logger;

    public PromptTransferService(IPromptLibraryService library, IPromptCollectionService collections, ILogger<PromptTransferService> logger)
    {
        _library = library;
        _collections = collections;
        _logger = logger;
    }

    // ───────────────────────────── Export ─────────────────────────────

    public async Task<Result<ExportFile>> ExportAsync(IReadOnlyCollection<Guid>? ids, string format, string flavor, CancellationToken ct = default)
    {
        flavor = flavor == PromptMarkdownSerializer.FlavorClaudeCommand ? flavor : PromptMarkdownSerializer.FlavorDiax;
        var collectionNames = (await _collections.ListAsync(ct)).ToDictionary(c => c.Id, c => c.Name);

        var targetIds = ids is { Count: > 0 }
            ? ids.Distinct().ToList()
            // Pede 1 a mais que o teto para detectar biblioteca grande demais em vez de cortar calado.
            : (await _library.ListAsync(new PromptListQuery { Sort = "title", Limit = MaxExportPrompts + 1 }, ct)).Select(p => p.Id).ToList();
        if (targetIds.Count == 0)
            return Result.Failure<ExportFile>(new Error("Prompt.Validation", "Nenhum prompt para exportar."));
        if (targetIds.Count > MaxExportPrompts)
            return Result.Failure<ExportFile>(new Error("Prompt.Validation", $"Máximo de {MaxExportPrompts} prompts por export."));

        // Lote (2 queries por 500 ids) em vez de N+1.
        var details = await _library.GetManyAsync(targetIds, ct);
        if (details.Count == 0)
            return Result.Failure<ExportFile>(new Error("Prompt.NotFound", "Nenhum dos prompts foi encontrado."));

        string? CollectionOf(PromptDetailDto d) =>
            d.Prompt.CollectionId is { } cid && collectionNames.TryGetValue(cid, out var n) ? n : null;

        if (format == "md" && details.Count == 1)
        {
            var d = details[0];
            var md = PromptMarkdownSerializer.Serialize(d, CollectionOf(d), flavor);
            return Result.Success(new ExportFile(PromptMarkdownSerializer.FileName(d.Prompt), "text/markdown; charset=utf-8", new UTF8Encoding(false).GetBytes(md)));
        }

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in details)
            {
                // Sabor Claude Code: arquivos na raiz (pasta = namespace de comando no Claude Code).
                var folder = flavor == PromptMarkdownSerializer.FlavorClaudeCommand ? "" : PromptTemplate.Slugify(CollectionOf(d) ?? "sem-colecao") + "/";
                var path = folder + PromptMarkdownSerializer.FileName(d.Prompt);
                for (var i = 2; !used.Add(path); i++) path = $"{folder}{d.Prompt.Slug}-{i}.md";

                var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
                await using var s = entry.Open();
                var bytes = new UTF8Encoding(false).GetBytes(PromptMarkdownSerializer.Serialize(d, CollectionOf(d), flavor));
                await s.WriteAsync(bytes, ct);
            }
        }

        var name = flavor == PromptMarkdownSerializer.FlavorClaudeCommand ? "prompt-studio-claude-commands" : "prompt-studio";
        return Result.Success(new ExportFile($"{name}-{DateTime.UtcNow:yyyyMMdd-HHmm}.zip", "application/zip", buffer.ToArray()));
    }

    // ───────────────────────────── Import ─────────────────────────────

    public async Task<Result<ImportReportDto>> ImportAsync(IReadOnlyList<ImportUpload> files, bool dryRun, string strategy, Guid userId, CancellationToken ct = default)
    {
        strategy = Strategies.Contains(strategy) ? strategy : "new-version";
        if (files.Count == 0)
            return Result.Failure<ImportReportDto>(new Error("Prompt.Validation", "Envie ao menos um arquivo .md, .txt ou .zip."));

        var items = new List<ImportItemDto>();
        var entries = new List<ImportUpload>();

        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            if (ext == ".zip")
            {
                var (zipEntries, zipError) = ExpandZip(f);
                if (zipError != null) items.Add(Error(f.FileName, zipError));
                entries.AddRange(zipEntries);
            }
            else if (ext is ".md" or ".markdown" or ".txt")
            {
                if (f.Content.LongLength > MaxFileBytes) items.Add(Error(f.FileName, "Arquivo maior que 512 KB."));
                else entries.Add(f);
            }
            else
            {
                items.Add(Error(f.FileName, "Formato não suportado (use .md, .txt ou .zip)."));
            }
        }

        var collectionIds = (await _collections.ListAsync(ct))
            .ToDictionary(c => c.Name, c => (Guid?)c.Id, StringComparer.OrdinalIgnoreCase);
        // Dry-run: slugs que "seriam criados" neste lote, para a prévia bater com a gravação real
        // (2º arquivo com o mesmo slug vira nova versão, não outro create).
        var plannedSlugs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            try
            {
                items.Add(await ImportOneAsync(entry, dryRun, strategy, userId, collectionIds, plannedSlugs, ct));
            }
            catch (FormatException ex)
            {
                items.Add(Error(entry.FileName, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha importando {File}", entry.FileName);
                items.Add(Error(entry.FileName, "Erro inesperado ao importar este arquivo."));
            }
        }

        var summary = new ImportSummaryDto(
            items.Count(i => i.Action == "create"),
            items.Count(i => i.Action == "newVersion"),
            items.Count(i => i.Action == "unchanged"),
            items.Count(i => i.Action == "skip"),
            items.Count(i => i.Action == "error"));
        if (!dryRun) _logger.LogInformation("prompt.imported {@Summary}", summary);
        return Result.Success(new ImportReportDto(dryRun, strategy, summary, items));
    }

    private async Task<ImportItemDto> ImportOneAsync(
        ImportUpload file, bool dryRun, string strategy, Guid userId,
        Dictionary<string, Guid?> collectionIds, HashSet<string> plannedSlugs, CancellationToken ct)
    {
        var text = DecodeUtf8(file.Content);
        var parsed = PromptMarkdownSerializer.Parse(file.FileName, text);
        var warnings = new List<string>(parsed.Warnings);

        if (parsed.Kind != null && !PromptLibraryService.TryParseKind(parsed.Kind, out _))
        {
            warnings.Add($"Tipo '{parsed.Kind}' desconhecido; importado como prompt.");
            parsed = parsed with { Kind = null };
        }

        // Match: id do bloco diax (se for deste usuário) → slug.
        PromptDetailDto? existing = null;
        if (parsed.Id is { } id)
        {
            var byId = await _library.GetAsync(id, ct);
            if (byId.IsSuccess) existing = byId.Value;
        }
        if (existing == null)
        {
            var bySlug = await _library.GetBySlugAsync(parsed.Slug, ct);
            if (bySlug.IsSuccess) existing = bySlug.Value;
        }

        if (dryRun && existing == null && strategy != "duplicate" && !plannedSlugs.Add(parsed.Slug))
        {
            var action = strategy == "skip" ? "skip" : "newVersion";
            return new ImportItemDto(file.FileName, action, parsed.Slug, parsed.Title, null,
                warnings.Append("Mesmo slug de outro arquivo deste lote.").ToList(), null);
        }

        if (existing != null && strategy != "duplicate")
        {
            if (strategy == "skip")
                return new ImportItemDto(file.FileName, "skip", existing.Prompt.Slug, existing.Prompt.Title, existing.Prompt.Id, warnings, null);

            var incomingVars = PromptLibraryService.ResolveVariables(parsed.Content, parsed.Variables);
            var sameContent = parsed.Content == existing.Current.Content
                && JsonSerializer.Serialize(incomingVars, Json) == JsonSerializer.Serialize(existing.Current.Variables, Json);
            if (sameContent)
                return new ImportItemDto(file.FileName, "unchanged", existing.Prompt.Slug, existing.Prompt.Title, existing.Prompt.Id, warnings, null);

            if (!dryRun)
            {
                var v = await _library.AddVersionAsync(existing.Prompt.Id, new AddPromptVersionRequest
                {
                    Content = parsed.Content,
                    Variables = parsed.Variables,
                    ChangeNote = $"Importado de {Path.GetFileName(file.FileName)}",
                    BaseVersionNumber = existing.Current.Number,
                    Source = "import",
                }, ct);
                if (v.IsFailure) return Error(file.FileName, v.Error.Message, warnings);
            }
            return new ImportItemDto(file.FileName, "newVersion", existing.Prompt.Slug, existing.Prompt.Title, existing.Prompt.Id, warnings, null);
        }

        // Criação (novo ou estratégia duplicate).
        Guid? collectionId = null;
        if (parsed.Collection != null)
        {
            if (collectionIds.TryGetValue(parsed.Collection, out var cid)) collectionId = cid;
            else if (dryRun) warnings.Add($"A coleção \"{parsed.Collection}\" será criada.");
            else
            {
                var created = await _collections.CreateAsync(new SavePromptCollectionRequest { Name = parsed.Collection }, userId, ct);
                if (created.IsSuccess)
                {
                    collectionId = created.Value!.Id;
                    collectionIds[parsed.Collection] = collectionId;
                }
                else warnings.Add($"Coleção \"{parsed.Collection}\" não criada: {created.Error.Message}");
            }
        }

        if (dryRun)
            return new ImportItemDto(file.FileName, "create", existing == null ? parsed.Slug : null, parsed.Title, null, warnings, null);

        var result = await _library.CreateAsync(new CreatePromptRequest
        {
            Title = parsed.Title,
            Slug = existing == null ? parsed.Slug : null,
            Description = parsed.Description,
            Content = parsed.Content,
            Variables = parsed.Variables,
            Tags = parsed.Tags,
            CollectionId = collectionId,
            Kind = parsed.Kind,
            Framework = parsed.Framework,
            TargetModel = parsed.TargetModel,
            ExposeToMcp = parsed.ExposeToMcp,
            IsFavorite = parsed.Favorite,
            ChangeNote = $"Importado de {Path.GetFileName(file.FileName)}",
            Source = "import",
        }, userId, ct);

        return result.IsSuccess
            ? new ImportItemDto(file.FileName, "create", result.Value!.Prompt.Slug, result.Value.Prompt.Title, result.Value.Prompt.Id, warnings, null)
            : Error(file.FileName, result.Error.Message, warnings);
    }

    /// <summary>Lê o zip em memória (nada vai para o disco → sem zip-slip), com limites contra zip bomb.</summary>
    private static (List<ImportUpload> Entries, string? Error) ExpandZip(ImportUpload zipFile)
    {
        var list = new List<ImportUpload>();
        if (zipFile.Content.LongLength > MaxZipBytes) return (list, "Zip maior que 5 MB.");
        try
        {
            using var zip = new ZipArchive(new MemoryStream(zipFile.Content), ZipArchiveMode.Read);
            var candidates = zip.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name) && !e.FullName.StartsWith("__MACOSX", StringComparison.Ordinal))
                .Where(e => Path.GetExtension(e.Name).ToLowerInvariant() is ".md" or ".markdown" or ".txt")
                .ToList();
            if (candidates.Count > MaxZipEntries) return (list, $"Zip com mais de {MaxZipEntries} arquivos.");

            long total = 0;
            foreach (var e in candidates)
            {
                if (e.Length > MaxFileBytes) { list.Add(new ImportUpload($"{zipFile.FileName}/{e.FullName}", Array.Empty<byte>())); continue; }
                total += e.Length;
                if (total > MaxUncompressedBytes) return (list, "Conteúdo descompactado passa de 20 MB.");
                // Não confia no tamanho declarado no diretório central: lê com teto real.
                var bytes = ReadBounded(e, MaxFileBytes);
                list.Add(bytes is null
                    ? new ImportUpload($"{zipFile.FileName}/{e.FullName}", Array.Empty<byte>())
                    : new ImportUpload(e.FullName, bytes));
            }
            return (list, candidates.Count == 0 ? "Zip sem arquivos .md ou .txt." : null);
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
        {
            // NotSupportedException: entrada criptografada ou método de compressão não suportado.
            return (list, "Arquivo .zip inválido, corrompido ou protegido por senha.");
        }
    }

    private static byte[]? ReadBounded(ZipArchiveEntry entry, long max)
    {
        using var s = entry.Open();
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (ms.Length + read > max) return null;
            ms.Write(buffer, 0, read);
        }
        return ms.ToArray();
    }

    private static string DecodeUtf8(byte[] bytes)
    {
        if (bytes.Length == 0) throw new FormatException("Arquivo vazio ou maior que 512 KB.");
        try { return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes); }
        catch (DecoderFallbackException) { throw new FormatException("O arquivo não está em UTF-8."); }
    }

    private static ImportItemDto Error(string file, string message, IReadOnlyList<string>? warnings = null)
        => new(file, "error", null, null, null, warnings ?? Array.Empty<string>(), message);
}
