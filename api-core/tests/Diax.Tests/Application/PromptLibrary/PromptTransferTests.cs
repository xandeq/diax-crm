using System.IO.Compression;
using System.Text;
using Diax.Application.PromptLibrary;
using Diax.Application.PromptLibrary.Dtos;
using Diax.Domain.Common;
using Diax.Domain.PromptGenerator;
using Diax.Domain.Snippets;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Diax.Tests.Application.PromptLibrary;

public class PromptMarkdownSerializerTests
{
    private static PromptDetailDto Detail(string content, IReadOnlyList<PromptVariableDto> vars, string? description = null) => new(
        new PromptListItemDto(Guid.NewGuid(), "revisor-de-codigo", "Revisor de código", description, "template", "risen", null,
            new[] { "dev", "review" }, "", vars.Select(v => v.Name).ToList(), true, false, true, 7, 0, null,
            DateTime.UtcNow, DateTime.UtcNow, "manual"),
        "claude-sonnet-5",
        new PromptVersionDto(7, content, vars, null, "manual", null, DateTime.UtcNow));

    private static readonly IReadOnlyList<PromptVariableDto> Vars = new[]
    {
        new PromptVariableDto("lang", "Linguagem", "select", "TypeScript", true, new[] { "TypeScript", "C#" }),
        new PromptVariableDto("codigo", "Código", "textarea", null, true),
    };

    [Fact]
    public void Diax_flavor_round_trips_exactly()
    {
        var content = "Revise em {{lang}}:\n\n```\n{{codigo}}\n```\n\nliteral \\{{x}} e $5,00\n";
        var md = PromptMarkdownSerializer.Serialize(Detail(content, Vars, "Revisão P0/P1"), "Dev");
        var parsed = PromptMarkdownSerializer.Parse("qualquer.md", md);

        Assert.Equal(content, parsed.Content);
        Assert.Equal("Revisor de código", parsed.Title);
        Assert.Equal("revisor-de-codigo", parsed.Slug);
        Assert.Equal("Revisão P0/P1", parsed.Description);
        Assert.Equal("Dev", parsed.Collection);
        Assert.Equal(new[] { "dev", "review" }, parsed.Tags);
        Assert.Equal("template", parsed.Kind);
        Assert.Equal("risen", parsed.Framework);
        Assert.True(parsed.ExposeToMcp);
        Assert.True(parsed.Favorite);
        Assert.Equal("claude-sonnet-5", parsed.TargetModel);
        Assert.Equal(Vars[0] with { Options = null }, parsed.Variables[0] with { Options = null });
        Assert.Equal(new[] { "TypeScript", "C#" }, parsed.Variables[0].Options);
        Assert.Equal("textarea", parsed.Variables[1].Type);
        Assert.NotNull(parsed.Id);
        Assert.Empty(parsed.Warnings);
    }

    [Fact]
    public void Description_equal_to_title_is_not_imported_as_description()
    {
        var md = PromptMarkdownSerializer.Serialize(Detail("x", Array.Empty<PromptVariableDto>()), null);
        Assert.StartsWith("---\ndescription: Revisor de código\n", md);
        Assert.Null(PromptMarkdownSerializer.Parse("a.md", md).Description);
    }

    [Fact]
    public void Claude_flavor_uses_positional_args_and_round_trips_variable_names()
    {
        var md = PromptMarkdownSerializer.Serialize(Detail("Revise {{codigo}} em {{lang}} e {{codigo}}", Vars), null, PromptMarkdownSerializer.FlavorClaudeCommand);

        Assert.Matches(@"argument-hint: ['""]\[lang\] \[codigo\]['""]", md);
        Assert.Contains("Revise $2 em $1 e $2", md);

        var parsed = PromptMarkdownSerializer.Parse("revisor.md", md);
        Assert.Equal("Revise {{codigo}} em {{lang}} e {{codigo}}", parsed.Content);
        Assert.Empty(parsed.Warnings);
    }

    [Fact]
    public void Claude_flavor_single_variable_uses_ARGUMENTS()
    {
        var one = new[] { new PromptVariableDto("diff") };
        var md = PromptMarkdownSerializer.Serialize(Detail("Commit para: {{diff}}", one), null, PromptMarkdownSerializer.FlavorClaudeCommand);
        Assert.Contains("Commit para: $ARGUMENTS", md);
        Assert.Equal("Commit para: {{diff}}", PromptMarkdownSerializer.Parse("c.md", md).Content);
    }

    [Fact]
    public void Plain_claude_command_without_diax_block_is_imported()
    {
        const string cmd = "---\ndescription: Revisa um PR\nargument-hint: \"[pr-number] [prioridade]\"\nallowed-tools: Bash(gh:*)\n---\nRevise o PR #$1 com prioridade $2.\nContexto extra: $ARGUMENTS\n";
        var parsed = PromptMarkdownSerializer.Parse("review-pr.md", cmd);

        Assert.Equal("Review pr", parsed.Title);
        Assert.Equal("review-pr", parsed.Slug);
        Assert.Equal("Revisa um PR", parsed.Description);
        Assert.Equal("Revise o PR #{{pr_number}} com prioridade {{prioridade}}.\nContexto extra: {{arguments}}", parsed.Content);
        Assert.Single(parsed.Warnings);
    }

    [Fact]
    public void Plain_text_file_uses_file_name_as_title()
    {
        var parsed = PromptMarkdownSerializer.Parse("Início de prompt padrao.txt", "﻿Atue como dev sênior.\r\nSeja breve.\r\n");
        Assert.Equal("Início de prompt padrao", parsed.Title);
        Assert.Equal("inicio-de-prompt-padrao", parsed.Slug);
        Assert.Equal("Atue como dev sênior.\nSeja breve.", parsed.Content);
    }

    [Fact]
    public void Empty_frontmatter_is_accepted()
    {
        var parsed = PromptMarkdownSerializer.Parse("vazio.md", "---\n---\ncorpo\n\n---\nnão é frontmatter\n");
        Assert.Equal("corpo\n\n---\nnão é frontmatter", parsed.Content);
    }

    [Theory]
    [InlineData("---\ndiax: [oops\n---\ncorpo", "linha")]
    [InlineData("---\ndescription: x\nsem fechamento", "não fechado")]
    [InlineData("---\ndescription: x\n---\n   \n", "sem conteúdo")]
    public void Invalid_files_raise_friendly_errors(string text, string expected)
    {
        var ex = Assert.Throws<FormatException>(() => PromptMarkdownSerializer.Parse("x.md", text));
        Assert.Contains(expected, ex.Message);
    }
}

public class PromptTransferServiceTests
{
    private sealed class FakeUser : ICurrentUserService
    {
        public Guid? UserId { get; init; }
        public bool IsAuthenticated => true;
    }

    private static (PromptTransferService Transfer, PromptLibraryService Library, PromptCollectionService Collections, Guid UserId) Create()
    {
        var user = new FakeUser { UserId = Guid.NewGuid() };
        var db = new DiaxDbContext(new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user);
        var uow = new UnitOfWork(db);
        var collectionsRepo = new PromptCollectionRepository(db);
        var library = new PromptLibraryService(new PromptRepository(db), collectionsRepo, new Mock<IUserPromptRepository>().Object,
            new Mock<ISnippetRepository>().Object, uow, NullLogger<PromptLibraryService>.Instance);
        var collections = new PromptCollectionService(collectionsRepo, uow);
        return (new PromptTransferService(library, collections, NullLogger<PromptTransferService>.Instance), library, collections, user.UserId!.Value);
    }

    private static ImportUpload Md(string name, string text) => new(name, new UTF8Encoding(false).GetBytes(text));

    private const string NewPromptMd = "---\ndescription: Cold email curto\ndiax:\n  title: Cold email\n  collection: Vendas\n  tags: [cold-email]\n  kind: template\n---\nOlá {{nome}}, tudo bem?\n";

    [Fact]
    public async Task Dry_run_reports_without_writing()
    {
        var (t, lib, col, uid) = Create();
        var report = await t.ImportAsync(new[] { Md("cold.md", NewPromptMd) }, dryRun: true, "new-version", uid);

        Assert.True(report.IsSuccess);
        var item = Assert.Single(report.Value!.Items);
        Assert.Equal("create", item.Action);
        Assert.Contains(item.Warnings, w => w.Contains("Vendas"));
        Assert.Empty(await lib.ListAsync(new PromptListQuery()));
        Assert.Empty(await col.ListAsync());
    }

    [Fact]
    public async Task Import_creates_prompt_and_collection_then_reimport_is_unchanged()
    {
        var (t, lib, col, uid) = Create();
        var first = await t.ImportAsync(new[] { Md("cold.md", NewPromptMd) }, dryRun: false, "new-version", uid);
        Assert.Equal(1, first.Value!.Summary.Create);

        var p = Assert.Single(await lib.ListAsync(new PromptListQuery()));
        Assert.Equal("Cold email", p.Title);
        Assert.Equal("import", p.Source);
        Assert.Equal("template", p.Kind);
        Assert.Equal(new[] { "cold-email" }, p.Tags);
        Assert.Equal("Vendas", (await col.ListAsync()).Single().Name);

        var again = await t.ImportAsync(new[] { Md("cold.md", NewPromptMd) }, dryRun: false, "new-version", uid);
        Assert.Equal(1, again.Value!.Summary.Unchanged);
        Assert.Single(await col.ListAsync());
    }

    [Fact]
    public async Task Strategies_new_version_skip_and_duplicate()
    {
        var (t, lib, _, uid) = Create();
        await t.ImportAsync(new[] { Md("cold.md", NewPromptMd) }, false, "new-version", uid);
        var changed = NewPromptMd.Replace("tudo bem?", "como vai?");

        Assert.Equal("skip", (await t.ImportAsync(new[] { Md("cold.md", changed) }, false, "skip", uid)).Value!.Items[0].Action);

        var nv = await t.ImportAsync(new[] { Md("cold.md", changed) }, false, "new-version", uid);
        Assert.Equal("newVersion", nv.Value!.Items[0].Action);
        var detail = await lib.GetAsync(nv.Value.Items[0].PromptId!.Value);
        Assert.Equal(2, detail.Value!.Current.Number);
        Assert.Equal("import", detail.Value.Current.Source);

        var dup = await t.ImportAsync(new[] { Md("cold.md", changed) }, false, "duplicate", uid);
        Assert.Equal("create", dup.Value!.Items[0].Action);
        Assert.Equal(2, (await lib.ListAsync(new PromptListQuery())).Count);
    }

    [Fact]
    public async Task Export_md_then_import_round_trips_through_the_service()
    {
        var (t, lib, col, uid) = Create();
        var dev = (await col.CreateAsync(new SavePromptCollectionRequest { Name = "Dev" }, uid)).Value!;
        var created = (await lib.CreateAsync(new CreatePromptRequest
        {
            Title = "Revisor", Content = "Revise {{codigo}}", Tags = new() { "dev" }, CollectionId = dev.Id,
            Variables = new() { new PromptVariableDto("codigo", "Código", "textarea") }
        }, uid)).Value!;

        var file = await t.ExportAsync(new[] { created.Prompt.Id }, "md", "diax");
        Assert.Equal("revisor.md", file.Value!.FileName);

        var report = await t.ImportAsync(new[] { new ImportUpload("revisor.md", file.Value.Content) }, false, "new-version", uid);
        Assert.Equal("unchanged", report.Value!.Items[0].Action); // casou pelo id do bloco diax
    }

    [Fact]
    public async Task Zip_export_and_import_with_errors_isolated_per_file()
    {
        var (t, lib, _, uid) = Create();
        await lib.CreateAsync(new CreatePromptRequest { Title = "A", Content = "a" }, uid);
        await lib.CreateAsync(new CreatePromptRequest { Title = "B", Content = "b {{x}}" }, uid);

        var zip = await t.ExportAsync(null, "zip", "claude-command");
        Assert.EndsWith(".zip", zip.Value!.FileName);
        using (var archive = new ZipArchive(new MemoryStream(zip.Value.Content)))
            Assert.Equal(new[] { "a.md", "b.md" }, archive.Entries.Select(e => e.FullName).OrderBy(x => x));

        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            void Add(string name, string text) { using var s = z.CreateEntry(name).Open(); s.Write(Encoding.UTF8.GetBytes(text)); }
            Add("../../fora.md", "conteúdo ok");
            Add("ruim.md", "---\ndiax: [quebrado\n---\nx");
            Add("imagem.png", "ignorado");
        }
        var report = await t.ImportAsync(new[] { new ImportUpload("lote.zip", ms.ToArray()) }, false, "new-version", uid);

        Assert.Equal(1, report.Value!.Summary.Create);
        Assert.Equal(1, report.Value.Summary.Errors);
        Assert.Contains(report.Value.Items, i => i.Action == "error" && i.Error!.Contains("YAML"));
    }

    [Fact]
    public async Task Dry_run_matches_real_import_for_duplicate_slugs_in_same_batch()
    {
        var (t, _, _, uid) = Create();
        var files = new[] { Md("a/cold.md", NewPromptMd), Md("b/cold.md", NewPromptMd.Replace("tudo bem?", "oi")) };

        var preview = await t.ImportAsync(files, true, "new-version", uid);
        var real = await t.ImportAsync(files, false, "new-version", uid);

        Assert.Equal(new[] { "create", "newVersion" }, preview.Value!.Items.Select(i => i.Action));
        Assert.Equal(new[] { "create", "newVersion" }, real.Value!.Items.Select(i => i.Action));
    }

    [Fact]
    public async Task Invalid_field_in_one_file_does_not_break_the_rest_of_the_batch()
    {
        var (t, lib, _, uid) = Create();
        var bad = "---\ndiax:\n  title: Ruim\n  targetModel: " + new string('m', 120) + "\n---\nconteúdo\n";
        var report = await t.ImportAsync(new[] { Md("ruim.md", bad), Md("bom.md", "conteúdo bom") }, false, "new-version", uid);

        Assert.Equal(new[] { "error", "create" }, report.Value!.Items.Select(i => i.Action));
        Assert.Contains("80", report.Value.Items[0].Error);
        Assert.Single(await lib.ListAsync(new PromptListQuery()));
    }

    [Fact]
    public async Task Export_rejects_ids_from_nowhere_and_batches_many()
    {
        var (t, lib, _, uid) = Create();
        for (var i = 0; i < 30; i++) await lib.CreateAsync(new CreatePromptRequest { Title = $"P {i:00}", Content = $"c{i}" }, uid);

        var all = await t.ExportAsync(null, "zip", "diax");
        using (var archive = new ZipArchive(new MemoryStream(all.Value!.Content)))
            Assert.Equal(30, archive.Entries.Count);

        Assert.True((await t.ExportAsync(new[] { Guid.NewGuid() }, "md", "diax")).IsFailure);
    }

    [Fact]
    public async Task Rejects_unsupported_and_oversized_files()
    {
        var (t, _, _, uid) = Create();
        var report = await t.ImportAsync(new[]
        {
            new ImportUpload("foto.png", new byte[] { 1, 2, 3 }),
            new ImportUpload("grande.md", new byte[PromptTransferService.MaxFileBytes + 1]),
            new ImportUpload("latin1.txt", new byte[] { 0x41, 0xE7, 0xE3 }),
        }, true, "new-version", uid);

        Assert.Equal(3, report.Value!.Summary.Errors);
    }

    [Fact]
    public async Task Imports_the_legacy_prompts_txt_files_from_the_repo()
    {
        var dir = FindRepoPromptsDir();
        if (dir is null) return; // fora do repo (ex.: CI com checkout parcial)

        var (t, lib, _, uid) = Create();
        var uploads = Directory.GetFiles(dir, "*.txt").Select(f => new ImportUpload(Path.GetFileName(f), File.ReadAllBytes(f))).ToList();
        var report = await t.ImportAsync(uploads, false, "new-version", uid);

        Assert.True(uploads.Count >= 5);
        Assert.Equal(0, report.Value!.Summary.Errors);
        Assert.Equal(uploads.Count, (await lib.ListAsync(new PromptListQuery())).Count);
    }

    private static string? FindRepoPromptsDir()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "prompts");
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.txt").Length > 0) return candidate;
        }
        return null;
    }
}
