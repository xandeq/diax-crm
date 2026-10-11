using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Diax.Domain.PromptLibrary;

/// <summary>
/// Regras puras de template do Prompt Studio: variáveis <c>{{nome}}</c>, render, slug e tags.
/// Espelhado em <c>crm-web/src/lib/promptTemplate.ts</c> — mantenha os dois em sincronia.
/// </summary>
public static class PromptTemplate
{
    // (\\)? captura o escape "\{{nome}}" → saída literal "{{nome}}".
    private static readonly Regex VariableRegex = new(
        @"(\\)?\{\{\s*([A-Za-z_][A-Za-z0-9_.-]{0,63})\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NonSlugChars = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex RepeatedDash = new("-{2,}", RegexOptions.Compiled);

    public const int MaxSlugLength = 80;
    public const int MaxTagLength = 40;

    /// <summary>Nomes de variáveis na ordem da primeira aparição, sem repetição, ignorando as escapadas.</summary>
    public static IReadOnlyList<string> ExtractVariables(string? content)
    {
        var names = new List<string>();
        if (string.IsNullOrEmpty(content)) return names;

        foreach (Match m in VariableRegex.Matches(content))
        {
            if (m.Groups[1].Success) continue;
            var name = m.Groups[2].Value;
            if (!names.Contains(name, StringComparer.Ordinal)) names.Add(name);
        }
        return names;
    }

    /// <summary>
    /// Substitui variáveis. Valor vazio/ausente usa o default; sem default, o placeholder fica
    /// intacto e o nome entra em <see cref="RenderResult.Missing"/>.
    /// </summary>
    public static RenderResult Render(
        string content,
        IReadOnlyDictionary<string, string?>? values,
        IReadOnlyDictionary<string, string?>? defaults = null)
    {
        var missing = new List<string>();
        var used = new HashSet<string>(StringComparer.Ordinal);

        var rendered = VariableRegex.Replace(content ?? string.Empty, m =>
        {
            var name = m.Groups[2].Value;
            if (m.Groups[1].Success) return "{{" + name + "}}";

            used.Add(name);
            string? value = null;
            if (values != null && values.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v)) value = v;
            else if (defaults != null && defaults.TryGetValue(name, out var d) && !string.IsNullOrEmpty(d)) value = d;

            if (value is null)
            {
                if (!missing.Contains(name, StringComparer.Ordinal)) missing.Add(name);
                return m.Value;
            }
            return value;
        });

        var unused = values?.Keys.Where(k => !used.Contains(k)).ToList() ?? new List<string>();
        return new RenderResult(rendered, missing, unused);
    }

    /// <summary>"Revisor de Código!" → "revisor-de-codigo". Nunca vazio.</summary>
    public static string Slugify(string? text)
    {
        var s = RemoveDiacritics((text ?? string.Empty).ToLowerInvariant());
        s = NonSlugChars.Replace(s, "-").Trim('-');
        if (s.Length > MaxSlugLength) s = s[..MaxSlugLength].Trim('-');
        return string.IsNullOrEmpty(s) ? "prompt" : s;
    }

    /// <summary>" #Cold Email " → "cold-email". Retorna null se ficar vazia.</summary>
    public static string? NormalizeTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        var s = tag.Trim().TrimStart('#').Trim().ToLowerInvariant();
        s = Whitespace.Replace(s, "-");
        s = RepeatedDash.Replace(s, "-").Trim('-');
        if (s.Length > MaxTagLength) s = s[..MaxTagLength].Trim('-');
        return s.Length == 0 ? null : s;
    }

    public static string ComputeHash(string content, string? variablesJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content + "\u0000" + (variablesJson ?? string.Empty)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string BuildPreview(string content, int maxLength)
    {
        var flat = Whitespace.Replace(content ?? string.Empty, " ").Trim();
        return flat.Length <= maxLength ? flat : flat[..maxLength];
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed record RenderResult(string Rendered, IReadOnlyList<string> Missing, IReadOnlyList<string> Unused);
