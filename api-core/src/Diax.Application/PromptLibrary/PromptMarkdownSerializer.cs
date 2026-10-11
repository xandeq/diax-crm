using System.Text;
using System.Text.RegularExpressions;
using Diax.Application.PromptLibrary.Dtos;
using Diax.Domain.PromptLibrary;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Diax.Application.PromptLibrary;

/// <summary>
/// Formato canônico de arquivo do Prompt Studio (ADR-08): Markdown + frontmatter YAML.
/// <c>description</c>/<c>argument-hint</c> são lidos pelo Claude Code; o bloco <c>diax</c> guarda o resto.
/// Sabor "claude-command" troca {{var}} por $ARGUMENTS (1 variável) ou $1..$N, para o arquivo
/// funcionar direto em ~/.claude/commands/.
/// </summary>
public static class PromptMarkdownSerializer
{
    public const string FlavorDiax = "diax";
    public const string FlavorClaudeCommand = "claude-command";

    private static readonly Regex VariableRegex = new(@"(?<!\\)\{\{\s*([A-Za-z_][A-Za-z0-9_.-]{0,63})\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex PositionalRegex = new(@"\$(ARGUMENTS|[1-9])\b", RegexOptions.Compiled);
    private static readonly Regex HintRegex = new(@"\[([^\]]+)\]", RegexOptions.Compiled);
    private static readonly Regex ArgumentHintRegex = new(@"^argument-hint:[ \t]*(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly ISerializer Yaml = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .DisableAliases()
        .Build();

    private static readonly IDeserializer YamlIn = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    // ───────────────────────────── Export ─────────────────────────────

    public static string Serialize(PromptDetailDto detail, string? collectionName, string flavor = FlavorDiax)
    {
        var p = detail.Prompt;
        var content = detail.Current.Content;
        var variables = detail.Current.Variables;
        string? argumentHint = null;

        if (flavor == FlavorClaudeCommand && variables.Count > 0)
        {
            var names = variables.Select(v => v.Name).ToList();
            content = VariableRegex.Replace(content, m =>
            {
                var idx = names.IndexOf(m.Groups[1].Value);
                return idx < 0 ? m.Value : names.Count == 1 ? "$ARGUMENTS" : "$" + (idx + 1);
            });
            argumentHint = string.Join(" ", names.Select(n => $"[{n}]"));
        }

        var front = new Frontmatter
        {
            Description = p.Description ?? p.Title,
            Diax = new DiaxMeta
            {
                Title = p.Title,
                Slug = p.Slug,
                Kind = p.Kind,
                Framework = p.Framework,
                Collection = collectionName,
                Tags = p.Tags.Count > 0 ? p.Tags.ToList() : null,
                TargetModel = detail.TargetModel,
                ExposeToMcp = p.ExposeToMcp ? true : null,
                Favorite = p.IsFavorite ? true : null,
                Variables = variables.Count > 0
                    ? variables.Select(v => new VariableMeta
                    {
                        Name = v.Name,
                        Label = v.Label,
                        Type = v.Type == "text" ? null : v.Type,
                        Default = v.Default,
                        Required = v.Required ? null : false,
                        Options = v.Options is { Count: > 0 } ? v.Options.ToList() : null,
                    }).ToList()
                    : null,
                Id = p.Id.ToString(),
                Version = detail.Current.Number,
                Flavor = flavor == FlavorClaudeCommand ? FlavorClaudeCommand : null,
            }
        };

        var sb = new StringBuilder();
        // Sempre 1 "\n" final extra: o import remove exatamente um → round-trip byte a byte.
        var yaml = Yaml.Serialize(front).Replace("\r\n", "\n");
        // argument-hint escrito à mão (chave com hífen; nomes de variável são ASCII seguros).
        if (argumentHint != null)
        {
            var afterDescription = yaml.IndexOf("\ndiax:", StringComparison.Ordinal) + 1;
            yaml = yaml.Insert(afterDescription, $"argument-hint: \"{argumentHint}\"\n");
        }
        sb.Append("---\n").Append(yaml).Append("---\n").Append(content).Append('\n');
        return sb.ToString();
    }

    public static string FileName(PromptListItemDto p) => $"{p.Slug}.md";

    // ───────────────────────────── Import ─────────────────────────────

    /// <summary>Lê um .md (com ou sem frontmatter) ou .txt. Lança <see cref="FormatException"/> com mensagem amigável.</summary>
    public static ParsedPrompt Parse(string fileName, string text)
    {
        text = (text ?? string.Empty).TrimStart('﻿').Replace("\r\n", "\n");
        var warnings = new List<string>();
        Frontmatter? front = null;
        string? argumentHint = null;
        var body = text;

        if (text.StartsWith("---\n"))
        {
            var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end < 0) throw new FormatException("Frontmatter aberto com '---' mas não fechado.");
            var yaml = text[4..end];
            var afterFence = text.IndexOf('\n', end + 4);
            body = afterFence < 0 ? string.Empty : text[(afterFence + 1)..];
            if (yaml.Length > 20_000) throw new FormatException("Frontmatter grande demais (máx. 20 KB).");
            // Chave com hífen: lida direto do texto (o alias do YamlDotNet sofre a naming convention).
            var hint = ArgumentHintRegex.Match(yaml);
            if (hint.Success) argumentHint = hint.Groups[1].Value.Trim().Trim('"', '\'');
            try
            {
                front = string.IsNullOrWhiteSpace(yaml) ? null : YamlIn.Deserialize<Frontmatter>(yaml);
            }
            catch (YamlException ex)
            {
                throw new FormatException($"Frontmatter YAML inválido (linha {ex.Start.Line + 1}): {FirstLine(ex.InnerException?.Message ?? ex.Message)}");
            }
        }

        var diax = front?.Diax;
        var variables = diax?.Variables?
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .Select(v => new PromptVariableDto(v.Name!.Trim(), v.Label, v.Type ?? "text", v.Default, v.Required ?? true, v.Options))
            .ToList() ?? new List<PromptVariableDto>();

        // $ARGUMENTS / $1..$9 → {{variáveis}}, só em commands do Claude Code (sem bloco diax ou
        // exportados no sabor claude-command). Em arquivos DIAX, "$5,00" é texto literal.
        var isClaudeCommand = diax == null || diax.Flavor == FlavorClaudeCommand;
        if (isClaudeCommand && PositionalRegex.IsMatch(body))
        {
            var hintNames = HintRegex.Matches(argumentHint ?? string.Empty)
                .Select(m => PromptTemplate.Slugify(m.Groups[1].Value).Replace('-', '_'))
                .ToList();
            var declared = variables.Select(v => v.Name).ToList();
            body = PositionalRegex.Replace(body, m =>
            {
                if (m.Groups[1].Value == "ARGUMENTS")
                    return "{{" + (declared.Count == 1 ? declared[0] : hintNames.Count == 1 ? hintNames[0] : "arguments") + "}}";
                var i = int.Parse(m.Groups[1].Value) - 1;
                var name = i < declared.Count ? declared[i] : i < hintNames.Count ? hintNames[i] : $"arg{i + 1}";
                return "{{" + name + "}}";
            });
            if (diax?.Flavor != FlavorClaudeCommand)
                warnings.Add("Argumentos $ARGUMENTS/$N convertidos em {{variáveis}}.");
        }

        if (string.IsNullOrWhiteSpace(body)) throw new FormatException("Arquivo sem conteúdo depois do frontmatter.");

        var title = FirstNonEmpty(diax?.Title, TitleFromFileName(fileName));
        Guid? id = Guid.TryParse(diax?.Id, out var g) ? g : null;

        return new ParsedPrompt(
            Title: title.Length > Prompt.MaxTitleLength ? title[..Prompt.MaxTitleLength] : title,
            Slug: string.IsNullOrWhiteSpace(diax?.Slug) ? PromptTemplate.Slugify(Path.GetFileNameWithoutExtension(fileName)) : PromptTemplate.Slugify(diax!.Slug),
            // No export, description = título quando não há descrição própria.
            Description: front?.Description == diax?.Title ? null : NullIfEmpty(front?.Description),
            Content: body.EndsWith('\n') ? body[..^1] : body,
            Variables: variables,
            Tags: diax?.Tags ?? new List<string>(),
            Collection: NullIfEmpty(diax?.Collection),
            Kind: NullIfEmpty(diax?.Kind),
            Framework: NullIfEmpty(diax?.Framework),
            TargetModel: NullIfEmpty(diax?.TargetModel),
            ExposeToMcp: diax?.ExposeToMcp ?? false,
            Favorite: diax?.Favorite ?? false,
            Id: id,
            Warnings: warnings);
    }

    private static string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty).Replace('-', ' ').Replace('_', ' ').Trim();
        if (name.Length == 0) return "Prompt importado";
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string FirstNonEmpty(params string?[] values) => values.First(v => !string.IsNullOrWhiteSpace(v))!.Trim();
    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string FirstLine(string s) => s.Split('\n')[0].Trim();

    // ───────────────────────────── YAML DTOs ─────────────────────────────

    private sealed class Frontmatter
    {
        public string? Description { get; set; }
        public DiaxMeta? Diax { get; set; }
    }

    private sealed class DiaxMeta
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Kind { get; set; }
        public string? Framework { get; set; }
        public string? Collection { get; set; }
        public List<string>? Tags { get; set; }
        public string? TargetModel { get; set; }
        public bool? ExposeToMcp { get; set; }
        public bool? Favorite { get; set; }
        public List<VariableMeta>? Variables { get; set; }
        public string? Id { get; set; }
        public int? Version { get; set; }
        public string? Flavor { get; set; }
    }

    private sealed class VariableMeta
    {
        public string? Name { get; set; }
        public string? Label { get; set; }
        public string? Type { get; set; }
        public string? Default { get; set; }
        public bool? Required { get; set; }
        public List<string>? Options { get; set; }
    }
}

public sealed record ParsedPrompt(
    string Title,
    string Slug,
    string? Description,
    string Content,
    List<PromptVariableDto> Variables,
    List<string> Tags,
    string? Collection,
    string? Kind,
    string? Framework,
    string? TargetModel,
    bool ExposeToMcp,
    bool Favorite,
    Guid? Id,
    List<string> Warnings);
