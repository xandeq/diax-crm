using Ganss.Xss;

namespace Diax.Application.Blog.Services;

public interface IHtmlSanitizerService
{
    string Sanitize(string html);
}

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        // Tags permitidas (whitelist)
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("b");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("i");
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedTags.Add("img");
        _sanitizer.AllowedTags.Add("blockquote");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("pre");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("hr");
        _sanitizer.AllowedTags.Add("div");
        _sanitizer.AllowedTags.Add("span");

        // Atributos permitidos (whitelist)
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");
        _sanitizer.AllowedAttributes.Add("title");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("id");

        // Apenas HTTPS/HTTP
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("http");

        // Remover atributos de eventos JavaScript
        _sanitizer.AllowedCssProperties.Clear();
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }
}
