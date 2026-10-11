using Diax.Domain.Common;

namespace Diax.Domain.PromptLibrary;

/// <summary>
/// Prompt da biblioteca (Prompt Studio). O conteúdo vive em <see cref="PromptVersion"/> (imutáveis);
/// aqui ficam metadados e dados denormalizados da versão atual para a lista.
/// </summary>
public class Prompt : AuditableEntity, IUserOwnedEntity
{
    public const int MaxTitleLength = 160;
    public const int MaxDescriptionLength = 500;
    public const int MaxContentLength = 100_000;
    public const int MaxTags = 20;
    public const int PreviewLength = 280;
    public const int MaxVariableNamesLength = 500;

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CollectionId { get; private set; }
    public PromptKind Kind { get; private set; }
    public string? Framework { get; private set; }
    public string? TargetModel { get; private set; }

    public int CurrentVersionNumber { get; private set; }
    public string CurrentContentHash { get; private set; } = string.Empty;
    public string Preview { get; private set; } = string.Empty;
    public string? VariableNames { get; private set; }

    public bool IsFavorite { get; private set; }
    public bool IsArchived { get; private set; }
    public bool ExposeToMcp { get; private set; }

    public PromptSource Source { get; private set; }
    public Guid? SourceRefId { get; private set; }

    public int UseCount { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private readonly List<PromptVersion> _versions = new();
    public IReadOnlyCollection<PromptVersion> Versions => _versions;

    private readonly List<PromptTag> _tags = new();
    public IReadOnlyCollection<PromptTag> Tags => _tags;

    public PromptCollection? Collection { get; private set; }

    private Prompt() { }

    public static Prompt Create(
        Guid userId,
        string title,
        string slug,
        string content,
        string? variablesJson,
        string? changeNote,
        PromptSource source,
        Guid? sourceRefId = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId é obrigatório.", nameof(userId));

        var prompt = new Prompt
        {
            UserId = userId,
            Source = source,
            SourceRefId = sourceRefId
        };
        prompt.Rename(title);
        prompt.ChangeSlug(slug);
        prompt.AppendVersion(content, variablesJson, changeNote, source, null);
        return prompt;
    }

    /// <summary>
    /// Cria uma nova versão. Retorna <c>null</c> se o conteúdo for idêntico ao atual (no-op).
    /// Lança <see cref="PromptVersionConflictException"/> se <paramref name="baseVersionNumber"/> estiver desatualizado.
    /// </summary>
    public PromptVersion? AddVersion(
        string content,
        string? variablesJson,
        string? changeNote,
        PromptSource source,
        int baseVersionNumber,
        string? frameworkApplied = null)
    {
        if (baseVersionNumber != CurrentVersionNumber)
            throw new PromptVersionConflictException(CurrentVersionNumber, baseVersionNumber);

        ValidateContent(content);
        if (PromptTemplate.ComputeHash(content, variablesJson) == CurrentContentHash) return null;

        return AppendVersion(content, variablesJson, changeNote, source, frameworkApplied);
    }

    private PromptVersion AppendVersion(string content, string? variablesJson, string? changeNote, PromptSource source, string? frameworkApplied)
    {
        ValidateContent(content);
        var number = CurrentVersionNumber + 1;
        var version = new PromptVersion(Id, number, content, variablesJson, changeNote, source, frameworkApplied);
        _versions.Add(version);

        CurrentVersionNumber = number;
        CurrentContentHash = version.ContentHash;
        Preview = PromptTemplate.BuildPreview(content, PreviewLength);
        var names = string.Join(",", PromptTemplate.ExtractVariables(content));
        VariableNames = names.Length == 0 ? null : (names.Length > MaxVariableNamesLength ? names[..MaxVariableNamesLength] : names);
        Touch();
        return version;
    }

    public void Rename(string title)
    {
        var t = (title ?? string.Empty).Trim();
        if (t.Length == 0) throw new ArgumentException("Título é obrigatório.", nameof(title));
        if (t.Length > MaxTitleLength) throw new ArgumentException($"Título deve ter no máximo {MaxTitleLength} caracteres.", nameof(title));
        Title = t;
        Touch();
    }

    public void ChangeSlug(string slug)
    {
        var s = PromptTemplate.Slugify(slug);
        Slug = s;
        Touch();
    }

    public void SetDescription(string? description)
    {
        var d = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (d is { Length: > MaxDescriptionLength }) throw new ArgumentException($"Descrição deve ter no máximo {MaxDescriptionLength} caracteres.", nameof(description));
        Description = d;
        Touch();
    }

    public void SetClassification(PromptKind kind, string? framework, string? targetModel)
    {
        Kind = kind;
        Framework = string.IsNullOrWhiteSpace(framework) ? null : framework.Trim().ToLowerInvariant();
        TargetModel = string.IsNullOrWhiteSpace(targetModel) ? null : targetModel.Trim();
        Touch();
    }

    public void MoveToCollection(Guid? collectionId)
    {
        CollectionId = collectionId;
        Touch();
    }

    public void SetFavorite(bool value) { IsFavorite = value; Touch(); }
    public void SetArchived(bool value) { IsArchived = value; Touch(); }
    public void SetExposeToMcp(bool value) { ExposeToMcp = value; Touch(); }

    /// <summary>Substitui as tags (normalizadas, sem duplicadas, máx. <see cref="MaxTags"/>).</summary>
    public void SetTags(IEnumerable<string>? tags)
    {
        var normalized = (tags ?? Enumerable.Empty<string>())
            .Select(PromptTemplate.NormalizeTag)
            .Where(t => t != null)
            .Select(t => t!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalized.Count > MaxTags) throw new ArgumentException($"Máximo de {MaxTags} tags por prompt.", nameof(tags));

        _tags.RemoveAll(t => !normalized.Contains(t.Name, StringComparer.Ordinal));
        foreach (var name in normalized.Where(n => _tags.All(t => t.Name != n)))
            _tags.Add(new PromptTag(Id, name));
        Touch();
    }

    public void RegisterUse()
    {
        UseCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Conteúdo é obrigatório.", nameof(content));
        if (content.Length > MaxContentLength) throw new ArgumentException($"Conteúdo deve ter no máximo {MaxContentLength} caracteres.", nameof(content));
    }

    private void Touch() => SetUpdated();
}

public sealed class PromptVersionConflictException : Exception
{
    public int CurrentVersionNumber { get; }
    public int BaseVersionNumber { get; }

    public PromptVersionConflictException(int current, int baseVersion)
        : base($"O prompt está na v{current}, mas a edição partiu da v{baseVersion}.")
    {
        CurrentVersionNumber = current;
        BaseVersionNumber = baseVersion;
    }
}
