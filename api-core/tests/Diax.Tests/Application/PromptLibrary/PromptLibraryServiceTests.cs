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

public class PromptLibraryServiceTests
{
    private sealed class FakeUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class Sut
    {
        public required DiaxDbContext Db { get; init; }
        public required PromptLibraryService Service { get; init; }
        public required PromptCollectionService Collections { get; init; }
        public required FakeUser User { get; init; }
    }

    private static Sut Create(string? dbName = null, Guid? userId = null)
    {
        var user = new FakeUser { UserId = userId ?? Guid.NewGuid() };
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        var db = new DiaxDbContext(options, user);
        var uow = new UnitOfWork(db);
        var prompts = new PromptRepository(db);
        var collections = new PromptCollectionRepository(db);
        return new Sut
        {
            Db = db,
            User = user,
            Service = new PromptLibraryService(prompts, collections, new Mock<IUserPromptRepository>().Object,
                new Mock<ISnippetRepository>().Object, uow, NullLogger<PromptLibraryService>.Instance),
            Collections = new PromptCollectionService(collections, uow)
        };
    }

    private static CreatePromptRequest Req(string title = "Revisor de código", string content = "Revise {{codigo}} em {{lang}}") => new()
    {
        Title = title,
        Content = content,
        Tags = new() { "Dev", "review" },
        Variables = new() { new PromptVariableDto("lang", "Linguagem", "text", "TypeScript", true) }
    };

    [Fact]
    public async Task Create_then_get_returns_v1_with_tags_and_resolved_variables()
    {
        var s = Create();
        var created = await s.Service.CreateAsync(Req(), s.User.UserId!.Value);

        Assert.True(created.IsSuccess, created.Error.Message);
        var d = created.Value!;
        Assert.Equal("revisor-de-codigo", d.Prompt.Slug);
        Assert.Equal(new[] { "dev", "review" }, d.Prompt.Tags);
        Assert.Equal(1, d.Current.Number);
        Assert.Equal(new[] { "codigo", "lang" }, d.Current.Variables.Select(v => v.Name));
        Assert.Equal("TypeScript", d.Current.Variables[1].Default);
        Assert.Equal("text", d.Current.Variables[0].Type); // inferida
    }

    [Fact]
    public async Task Create_generates_unique_slug()
    {
        var s = Create();
        await s.Service.CreateAsync(Req(), s.User.UserId!.Value);
        var second = await s.Service.CreateAsync(Req(), s.User.UserId!.Value);
        Assert.Equal("revisor-de-codigo-2", second.Value!.Prompt.Slug);
    }

    [Fact]
    public async Task Large_content_round_trips_without_truncation()
    {
        var s = Create();
        var big = new string('x', 20_000) + " {{fim}}";
        var created = await s.Service.CreateAsync(Req(content: big), s.User.UserId!.Value);
        var fetched = await s.Service.GetAsync(created.Value!.Prompt.Id);
        Assert.Equal(big, fetched.Value!.Current.Content);
    }

    [Fact]
    public async Task AddVersion_creates_noop_and_conflict()
    {
        var s = Create();
        var id = (await s.Service.CreateAsync(Req(), s.User.UserId!.Value)).Value!.Prompt.Id;

        var v2 = await s.Service.AddVersionAsync(id, new AddPromptVersionRequest { Content = "novo {{x}}", BaseVersionNumber = 1, ChangeNote = "mudou" });
        Assert.True(v2.IsSuccess, v2.Error.Message);
        Assert.True(v2.Value!.Created);
        Assert.Equal(2, v2.Value.Version.Number);

        var same = await s.Service.AddVersionAsync(id, new AddPromptVersionRequest { Content = "novo {{x}}", BaseVersionNumber = 2 });
        Assert.False(same.Value!.Created);

        var stale = await s.Service.AddVersionAsync(id, new AddPromptVersionRequest { Content = "outro", BaseVersionNumber = 1 });
        Assert.Equal(PromptLibraryService.ErrConflict, stale.Error.Code);
    }

    [Fact]
    public async Task Restore_creates_new_version_and_keeps_history()
    {
        var s = Create();
        var id = (await s.Service.CreateAsync(Req(content: "original"), s.User.UserId!.Value)).Value!.Prompt.Id;
        await s.Service.AddVersionAsync(id, new AddPromptVersionRequest { Content = "segunda", BaseVersionNumber = 1 });

        var restored = await s.Service.RestoreVersionAsync(id, 1);

        Assert.Equal(3, restored.Value!.Version.Number);
        Assert.Equal("original", restored.Value.Version.Content);
        var versions = await s.Service.GetVersionsAsync(id);
        Assert.Equal(new[] { 3, 2, 1 }, versions.Value!.Select(v => v.Number));
    }

    [Fact]
    public async Task Metadata_update_does_not_create_version()
    {
        var s = Create();
        var id = (await s.Service.CreateAsync(Req(), s.User.UserId!.Value)).Value!.Prompt.Id;

        var updated = await s.Service.UpdateAsync(id, new UpdatePromptRequest { Title = "Novo título", IsFavorite = true, Tags = new() { "x" } });

        Assert.Equal("Novo título", updated.Value!.Prompt.Title);
        Assert.True(updated.Value.Prompt.IsFavorite);
        Assert.Equal(new[] { "x" }, updated.Value.Prompt.Tags);
        Assert.Equal(1, updated.Value.Prompt.CurrentVersionNumber);
    }

    [Fact]
    public async Task List_filters_by_tag_favorite_query_and_hides_archived()
    {
        var s = Create();
        var uid = s.User.UserId!.Value;
        var a = (await s.Service.CreateAsync(new CreatePromptRequest { Title = "Cold email", Content = "agência {{nome}}", Tags = new() { "vendas" }, IsFavorite = true }, uid)).Value!;
        await s.Service.CreateAsync(new CreatePromptRequest { Title = "Commit", Content = "conventional commits", Tags = new() { "dev" } }, uid);
        var archived = (await s.Service.CreateAsync(new CreatePromptRequest { Title = "Velho", Content = "x" }, uid)).Value!;
        await s.Service.UpdateAsync(archived.Prompt.Id, new UpdatePromptRequest { IsArchived = true });

        Assert.Equal(2, (await s.Service.ListAsync(new PromptListQuery())).Count);
        Assert.Single(await s.Service.ListAsync(new PromptListQuery { Archived = true }));
        Assert.Equal(a.Prompt.Id, (await s.Service.ListAsync(new PromptListQuery { Tag = "Vendas" })).Single().Id);
        Assert.Equal(a.Prompt.Id, (await s.Service.ListAsync(new PromptListQuery { Favorite = true })).Single().Id);
        // termo só no conteúdo da versão atual
        Assert.Single(await s.Service.ListAsync(new PromptListQuery { Q = "conventional" }));
    }

    [Fact]
    public async Task Search_ignores_content_only_present_in_old_versions()
    {
        var s = Create();
        var id = (await s.Service.CreateAsync(new CreatePromptRequest { Title = "T", Content = "palavraantiga" }, s.User.UserId!.Value)).Value!.Prompt.Id;
        await s.Service.AddVersionAsync(id, new AddPromptVersionRequest { Content = "texto novo", BaseVersionNumber = 1 });

        Assert.Empty(await s.Service.ListAsync(new PromptListQuery { Q = "palavraantiga" }));
    }

    [Fact]
    public async Task Render_applies_defaults_and_strict_mode()
    {
        var s = Create();
        var id = (await s.Service.CreateAsync(Req(), s.User.UserId!.Value)).Value!.Prompt.Id;

        var lax = await s.Service.RenderAsync(id, new RenderPromptRequest());
        Assert.Equal("Revise {{codigo}} em TypeScript", lax.Value!.Rendered);
        Assert.Equal(new[] { "codigo" }, lax.Value.Missing);

        var strict = await s.Service.RenderAsync(id, new RenderPromptRequest { Strict = true });
        Assert.Equal(PromptLibraryService.ErrValidation, strict.Error.Code);

        var ok = await s.Service.RenderAsync(id, new RenderPromptRequest { Variables = new() { ["codigo"] = "x()" } });
        Assert.Equal("Revise x() em TypeScript", ok.Value!.Rendered);
    }

    [Fact]
    public async Task Users_are_isolated()
    {
        var dbName = Guid.NewGuid().ToString();
        var owner = Create(dbName);
        var id = (await owner.Service.CreateAsync(Req(), owner.User.UserId!.Value)).Value!.Prompt.Id;

        var intruder = Create(dbName);
        Assert.Empty(await intruder.Service.ListAsync(new PromptListQuery()));
        Assert.Equal(PromptLibraryService.ErrNotFound, (await intruder.Service.GetAsync(id)).Error.Code);
        Assert.True((await intruder.Service.GetVersionsAsync(id)).IsFailure);
        Assert.True((await intruder.Service.RenderAsync(id, new RenderPromptRequest())).IsFailure);
        Assert.True((await intruder.Service.UpdateAsync(id, new UpdatePromptRequest { Title = "hack" })).IsFailure);
        Assert.True((await intruder.Service.DeleteAsync(id)).IsFailure);
    }

    [Fact]
    public async Task Tags_rename_and_counts()
    {
        var s = Create();
        var uid = s.User.UserId!.Value;
        await s.Service.CreateAsync(new CreatePromptRequest { Title = "A", Content = "a", Tags = new() { "old" } }, uid);
        await s.Service.CreateAsync(new CreatePromptRequest { Title = "B", Content = "b", Tags = new() { "old", "keep" } }, uid);

        var renamed = await s.Service.RenameTagAsync(new RenameTagRequest { From = "old", To = "New" });

        Assert.Equal(2, renamed.Value);
        var tags = await s.Service.GetTagsAsync();
        Assert.Contains(tags, t => t.Name == "new" && t.Count == 2);
        Assert.DoesNotContain(tags, t => t.Name == "old");
    }

    [Fact]
    public async Task Collections_crud_and_prompt_assignment()
    {
        var s = Create();
        var uid = s.User.UserId!.Value;
        var dev = (await s.Collections.CreateAsync(new SavePromptCollectionRequest { Name = "Dev", Color = "green" }, uid)).Value!;
        Assert.True((await s.Collections.CreateAsync(new SavePromptCollectionRequest { Name = "dev" }, uid)).IsFailure);

        var p = (await s.Service.CreateAsync(new CreatePromptRequest { Title = "P", Content = "c", CollectionId = dev.Id }, uid)).Value!;
        Assert.Equal(dev.Id, p.Prompt.CollectionId);
        Assert.Equal(1, (await s.Collections.ListAsync()).Single().PromptCount);

        var cleared = await s.Service.UpdateAsync(p.Prompt.Id, new UpdatePromptRequest { ClearCollection = true });
        Assert.Null(cleared.Value!.Prompt.CollectionId);

        Assert.True((await s.Service.CreateAsync(new CreatePromptRequest { Title = "X", Content = "c", CollectionId = Guid.NewGuid() }, uid)).IsFailure);
    }

    [Fact]
    public async Task FromGenerator_is_idempotent()
    {
        var s = Create();
        var uid = s.User.UserId!.Value;
        var history = new UserPrompt(uid, "Quero um prompt de cold email\nmais detalhes", "PROMPT FINAL {{nome}}", "costar", "anthropic", "claude-sonnet-5");
        var generator = new Mock<IUserPromptRepository>();
        generator.Setup(r => r.GetByIdWithUserAsync(history.Id, uid, It.IsAny<CancellationToken>())).ReturnsAsync(history);
        var service = new PromptLibraryService(new PromptRepository(s.Db), new PromptCollectionRepository(s.Db), generator.Object,
            new Mock<ISnippetRepository>().Object, new UnitOfWork(s.Db), NullLogger<PromptLibraryService>.Instance);

        var first = await service.CreateFromGeneratorAsync(history.Id, uid);
        var second = await service.CreateFromGeneratorAsync(history.Id, uid);

        Assert.True(first.IsSuccess, first.Error.Message);
        Assert.Equal("Quero um prompt de cold email", first.Value!.Prompt.Title);
        Assert.Equal("costar", first.Value.Prompt.Framework);
        Assert.Equal("generator", first.Value.Prompt.Source);
        Assert.Equal(first.Value.Prompt.Id, second.Value!.Prompt.Id);
        Assert.Single(await service.ListAsync(new PromptListQuery()));
    }

    [Fact]
    public async Task FromSnippet_only_accepts_text_or_markdown()
    {
        var s = Create();
        var uid = s.User.UserId!.Value;
        var md = new Snippet("Meu prompt", "Atue como {{papel}}", "markdown", false, uid);
        var cs = new Snippet("Código", "var x = 1;", "csharp", false, uid);
        var repo = new Mock<ISnippetRepository>();
        repo.Setup(r => r.GetByIdWithUserAsync(md.Id, uid, It.IsAny<CancellationToken>())).ReturnsAsync(md);
        repo.Setup(r => r.GetByIdWithUserAsync(cs.Id, uid, It.IsAny<CancellationToken>())).ReturnsAsync(cs);
        var service = new PromptLibraryService(new PromptRepository(s.Db), new PromptCollectionRepository(s.Db),
            new Mock<IUserPromptRepository>().Object, repo.Object, new UnitOfWork(s.Db), NullLogger<PromptLibraryService>.Instance);

        var ok = await service.CreateFromSnippetAsync(md.Id, uid);
        Assert.True(ok.IsSuccess, ok.Error.Message);
        Assert.Equal(new[] { "papel" }, ok.Value!.Prompt.VariableNames);
        Assert.Equal(PromptLibraryService.ErrValidation, (await service.CreateFromSnippetAsync(cs.Id, uid)).Error.Code);
    }
}
