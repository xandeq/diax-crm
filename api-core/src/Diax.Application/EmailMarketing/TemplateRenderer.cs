using System.Text.RegularExpressions;

namespace Diax.Application.EmailMarketing;

/// <summary>
/// Substitui variáveis no formato {{VariableName}} em templates de email.
/// Thread-safe (apenas operações de string imutáveis).
/// </summary>
public static class TemplateRenderer
{
    // Suporta: {{FirstName}}, {{COMPANY}}, {{my_var}} — case-insensitive, underscore/hifem
    private static readonly Regex TokenPattern =
        new(@"\{\{([A-Za-z0-9_\-]+)\}\}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Aplica substituição de variáveis em todas as partes de texto do template.
    /// Variáveis sem match são mantidas como estão (não geram erro).
    /// </summary>
    public static string Render(string template, IReadOnlyDictionary<string, string>? variables)
    {
        if (string.IsNullOrEmpty(template) || variables is null || variables.Count == 0)
            return template;

        // Build lookup case-insensitive para tolerância a maiúsculas/minúsculas
        var lookup = new Dictionary<string, string>(variables, StringComparer.OrdinalIgnoreCase);

        return TokenPattern.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return lookup.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    /// <summary>
    /// Aplica variáveis em HTML, texto e assunto de uma vez.
    /// Retorna novo (html, text, subject) — não modifica in-place.
    /// </summary>
    public static (string Html, string? Text, string Subject) RenderAll(
        string html,
        string? text,
        string subject,
        IReadOnlyDictionary<string, string>? variables)
    {
        if (variables is null || variables.Count == 0)
            return (html, text, subject);

        return (
            Render(html, variables),
            text is null ? null : Render(text, variables),
            Render(subject, variables)
        );
    }
}
