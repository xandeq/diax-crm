using Diax.Application.EmailMarketing;

namespace Diax.Tests.Application.EmailMarketing;

// 21/10: valores de lead (Google Maps/Extrator, terceiros) entravam crus no HTML do email e
// na prévia do CRM (dangerouslySetInnerHTML) → XSS com o JWT do admin em sessionStorage.
public class EmailTemplateEngineHtmlTests
{
    private readonly EmailTemplateEngine _engine = new();

    [Fact]
    public void RenderHtml_EscapesLeadValues()
    {
        var html = _engine.RenderHtml("<p>Olá {{nome}}, da {{empresa}}</p>",
            new Dictionary<string, string?>
            {
                ["nome"] = "<img src=x onerror=alert(1)>",
                ["empresa"] = "José & Filhos \"Ltda\""
            });

        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html);
        Assert.Contains("José &amp; Filhos &quot;Ltda&quot;", html); // acento intacto
        Assert.StartsWith("<p>Olá ", html);                            // template do dono intacto
    }

    [Fact]
    public void RenderHtml_UrlVariableStaysValidInsideHref()
    {
        var html = _engine.RenderHtml("<a href=\"{{unsubscribe_url}}\">sair</a>",
            new Dictionary<string, string?> { ["unsubscribe_url"] = "https://api.x/unsubscribe?token=a&uid=b" });

        Assert.Equal("<a href=\"https://api.x/unsubscribe?token=a&amp;uid=b\">sair</a>", html);
    }

    [Fact]
    public void Render_PlainText_IsUnchanged()
    {
        var subject = _engine.Render("Oi {{empresa}}", new Dictionary<string, string?> { ["empresa"] = "A & B" });

        Assert.Equal("Oi A & B", subject);
    }
}
