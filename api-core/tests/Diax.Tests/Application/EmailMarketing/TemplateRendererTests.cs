using Diax.Application.EmailMarketing;

namespace Diax.Tests.Application.EmailMarketing;

public class TemplateRendererTests
{
    [Fact]
    public void Render_SubstitutesKnownToken()
    {
        var result = TemplateRenderer.Render("Olá, {{FirstName}}!", new Dictionary<string, string> { ["FirstName"] = "João" });
        Assert.Equal("Olá, João!", result);
    }

    [Fact]
    public void Render_KeepsUnknownTokensUnchanged()
    {
        var result = TemplateRenderer.Render("Empresa: {{Company}}", new Dictionary<string, string>());
        Assert.Equal("Empresa: {{Company}}", result);
    }

    [Fact]
    public void Render_IsCaseInsensitive()
    {
        var result = TemplateRenderer.Render("{{firstname}}", new Dictionary<string, string> { ["FirstName"] = "Maria" });
        Assert.Equal("Maria", result);
    }

    [Fact]
    public void Render_NullVariablesReturnsOriginal()
    {
        var result = TemplateRenderer.Render("Olá, {{Name}}!", null);
        Assert.Equal("Olá, {{Name}}!", result);
    }

    [Fact]
    public void Render_EmptyVariablesReturnsOriginal()
    {
        var result = TemplateRenderer.Render("Olá, {{Name}}!", new Dictionary<string, string>());
        Assert.Equal("Olá, {{Name}}!", result);
    }

    [Fact]
    public void Render_EmptyTemplateReturnsEmpty()
    {
        var result = TemplateRenderer.Render("", new Dictionary<string, string> { ["Name"] = "X" });
        Assert.Equal("", result);
    }

    [Fact]
    public void Render_MultipleTokensInSameTemplate()
    {
        var vars = new Dictionary<string, string>
        {
            ["FirstName"] = "Carlos",
            ["Company"] = "ACME"
        };
        var result = TemplateRenderer.Render("<p>{{FirstName}} da {{Company}}</p>", vars);
        Assert.Equal("<p>Carlos da ACME</p>", result);
    }

    [Fact]
    public void RenderAll_AppliesVariablesToAllParts()
    {
        var vars = new Dictionary<string, string> { ["Name"] = "Ana" };
        var (html, text, subject) = TemplateRenderer.RenderAll(
            "<p>Olá {{Name}}</p>",
            "Olá {{Name}}",
            "Bem-vindo, {{Name}}",
            vars);

        Assert.Equal("<p>Olá Ana</p>", html);
        Assert.Equal("Olá Ana", text);
        Assert.Equal("Bem-vindo, Ana", subject);
    }

    [Fact]
    public void RenderAll_NullTextStaysNull()
    {
        var vars = new Dictionary<string, string> { ["X"] = "v" };
        var (_, text, _) = TemplateRenderer.RenderAll("<p>{{X}}</p>", null, "subj", vars);
        Assert.Null(text);
    }

    [Fact]
    public void RenderAll_NullVariablesReturnsOriginals()
    {
        var (html, text, subject) = TemplateRenderer.RenderAll("<p>{{A}}</p>", "{{A}}", "{{A}}", null);
        Assert.Equal("<p>{{A}}</p>", html);
        Assert.Equal("{{A}}", text);
        Assert.Equal("{{A}}", subject);
    }
}
