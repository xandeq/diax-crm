using Diax.Domain.PromptLibrary;
using Xunit;

namespace Diax.Tests.Application.PromptLibrary;

/// <summary>
/// Vetores de render. Os mesmos casos estão em crm-web/src/lib/promptTemplate.ts (TEMPLATE_VECTORS);
/// se mudar um, mude o outro.
/// </summary>
public class PromptTemplateTests
{
    [Theory]
    [InlineData("Olá {{nome}}", "nome=Ana", "Olá Ana", "")]
    [InlineData("{{ nome }} e {{nome}}", "nome=Ana", "Ana e Ana", "")]
    [InlineData("Oi {{nome}}", "", "Oi {{nome}}", "nome")]
    [InlineData("Literal \\{{nome}}", "nome=Ana", "Literal {{nome}}", "")]
    [InlineData("{{a}}{{b.c}}{{d-e}}", "a=1;b.c=2;d-e=3", "123", "")]
    [InlineData("{{1x}} fica", "", "{{1x}} fica", "")]
    [InlineData("Ação: {{ação}}", "", "Ação: {{ação}}", "")]
    [InlineData("Emoji {{x}} 🚀", "x=✓", "Emoji ✓ 🚀", "")]
    [InlineData("{{constructor}} {{toString}} {{__proto__}}", "", "{{constructor}} {{toString}} {{__proto__}}", "constructor,toString,__proto__")]
    public void Render_matches_vectors(string content, string values, string expected, string missing)
    {
        var dict = values.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(kv => kv.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => (string?)kv[1]);

        var result = PromptTemplate.Render(content, dict);

        Assert.Equal(expected, result.Rendered);
        Assert.Equal(missing, string.Join(",", result.Missing));
    }

    [Fact]
    public void Render_uses_default_when_value_empty()
    {
        var result = PromptTemplate.Render("Linguagem: {{lang}}",
            new Dictionary<string, string?> { ["lang"] = "" },
            new Dictionary<string, string?> { ["lang"] = "TypeScript" });

        Assert.Equal("Linguagem: TypeScript", result.Rendered);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void Render_reports_unused_values()
    {
        var result = PromptTemplate.Render("{{a}}", new Dictionary<string, string?> { ["a"] = "1", ["z"] = "9" });
        Assert.Equal(new[] { "z" }, result.Unused);
    }

    [Fact]
    public void ExtractVariables_keeps_first_appearance_order_and_ignores_escaped()
    {
        var vars = PromptTemplate.ExtractVariables("{{b}} {{a}} {{b}} \\{{c}}");
        Assert.Equal(new[] { "b", "a" }, vars);
    }

    [Theory]
    [InlineData(" Cold Email ", "cold-email")]
    [InlineData("#Dev", "dev")]
    [InlineData("cold-email", "cold-email")]
    [InlineData("  a   b  ", "a-b")]
    [InlineData("   ", null)]
    [InlineData("#", null)]
    public void NormalizeTag(string input, string? expected)
        => Assert.Equal(expected, PromptTemplate.NormalizeTag(input));

    [Theory]
    [InlineData("Revisor de Código!", "revisor-de-codigo")]
    [InlineData("  ", "prompt")]
    [InlineData("Ação & Reação 2.0", "acao-reacao-2-0")]
    public void Slugify(string input, string expected)
        => Assert.Equal(expected, PromptTemplate.Slugify(input));

    [Fact]
    public void Slugify_limits_length()
        => Assert.True(PromptTemplate.Slugify(new string('a', 200)).Length <= PromptTemplate.MaxSlugLength);
}

public class PromptDomainTests
{
    private static Prompt NewPrompt(string content = "v1 {{x}}")
        => Prompt.Create(Guid.NewGuid(), "Título", "titulo", content, null, "inicial", PromptSource.Manual);

    [Fact]
    public void Create_makes_version_1_with_denormalized_fields()
    {
        var p = NewPrompt("Olá {{nome}}, tudo bem com {{empresa}}?");

        Assert.Equal(1, p.CurrentVersionNumber);
        Assert.Single(p.Versions);
        Assert.Equal("nome,empresa", p.VariableNames);
        Assert.StartsWith("Olá", p.Preview);
        Assert.Equal(64, p.CurrentContentHash.Length);
    }

    [Fact]
    public void AddVersion_increments_number()
    {
        var p = NewPrompt();
        var v = p.AddVersion("v2", null, "nota", PromptSource.Manual, baseVersionNumber: 1);

        Assert.NotNull(v);
        Assert.Equal(2, v!.Number);
        Assert.Equal(2, p.CurrentVersionNumber);
        Assert.Null(p.VariableNames);
    }

    [Fact]
    public void AddVersion_with_same_content_is_noop()
    {
        var p = NewPrompt("igual");
        Assert.Null(p.AddVersion("igual", null, null, PromptSource.Manual, 1));
        Assert.Equal(1, p.CurrentVersionNumber);
    }

    [Fact]
    public void AddVersion_with_stale_base_throws_conflict()
    {
        var p = NewPrompt();
        p.AddVersion("v2", null, null, PromptSource.Manual, 1);

        var ex = Assert.Throws<PromptVersionConflictException>(() => p.AddVersion("v3", null, null, PromptSource.Mcp, 1));
        Assert.Equal(2, ex.CurrentVersionNumber);
    }

    [Fact]
    public void SetTags_normalizes_and_dedupes()
    {
        var p = NewPrompt();
        p.SetTags(new[] { " Cold Email ", "cold-email", "#Dev", "" });
        Assert.Equal(new[] { "cold-email", "dev" }, p.Tags.Select(t => t.Name).OrderBy(x => x));
    }

    [Fact]
    public void SetTags_rejects_more_than_limit()
    {
        var p = NewPrompt();
        Assert.Throws<ArgumentException>(() => p.SetTags(Enumerable.Range(0, Prompt.MaxTags + 1).Select(i => "t" + i)));
    }

    [Fact]
    public void Content_over_limit_is_rejected()
        => Assert.Throws<ArgumentException>(() => NewPrompt(new string('x', Prompt.MaxContentLength + 1)));
}
