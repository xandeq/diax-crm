using Diax.Domain.Common;

namespace Diax.Domain.PromptLibrary;

/// <summary>Snapshot imutável do conteúdo de um prompt. Restaurar = criar nova versão.</summary>
public class PromptVersion : Entity
{
    public const int MaxChangeNoteLength = 300;

    public Guid PromptId { get; private set; }
    public int Number { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? VariablesJson { get; private set; }
    public string? ChangeNote { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public PromptSource Source { get; private set; }
    public string? FrameworkApplied { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PromptVersion() { }

    internal PromptVersion(Guid promptId, int number, string content, string? variablesJson, string? changeNote, PromptSource source, string? frameworkApplied)
    {
        PromptId = promptId;
        Number = number;
        Content = content;
        VariablesJson = string.IsNullOrWhiteSpace(variablesJson) ? null : variablesJson;
        var note = string.IsNullOrWhiteSpace(changeNote) ? null : changeNote.Trim();
        ChangeNote = note is { Length: > MaxChangeNoteLength } ? note[..MaxChangeNoteLength] : note;
        Source = source;
        FrameworkApplied = Prompt.Limited(string.IsNullOrWhiteSpace(frameworkApplied) ? null : frameworkApplied.Trim().ToLowerInvariant(), Prompt.MaxFrameworkLength, "Framework");
        ContentHash = PromptTemplate.ComputeHash(content, VariablesJson);
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>Tag de um prompt (nome normalizado). PK composta (PromptId, Name).</summary>
public class PromptTag
{
    public Guid PromptId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private PromptTag() { }

    internal PromptTag(Guid promptId, string name)
    {
        PromptId = promptId;
        Name = name;
    }
}

/// <summary>Coleção (categoria de um nível) de prompts.</summary>
public class PromptCollection : AuditableEntity, IUserOwnedEntity
{
    public const int MaxNameLength = 60;

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }

    private PromptCollection() { }

    public PromptCollection(Guid userId, string name, string? icon, string? color, int sortOrder)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId é obrigatório.", nameof(userId));
        UserId = userId;
        Update(name, icon, color);
        SortOrder = sortOrder;
    }

    public void Update(string name, string? icon, string? color)
    {
        var n = (name ?? string.Empty).Trim();
        if (n.Length == 0) throw new ArgumentException("Nome da coleção é obrigatório.", nameof(name));
        if (n.Length > MaxNameLength) throw new ArgumentException($"Nome deve ter no máximo {MaxNameLength} caracteres.", nameof(name));
        Name = n;
        Icon = Prompt.Limited(string.IsNullOrWhiteSpace(icon) ? null : icon.Trim(), 40, "Ícone");
        Color = Prompt.Limited(string.IsNullOrWhiteSpace(color) ? null : color.Trim(), 16, "Cor");
        SetUpdated();
    }

    public void SetSortOrder(int order) { SortOrder = order; SetUpdated(); }
}
