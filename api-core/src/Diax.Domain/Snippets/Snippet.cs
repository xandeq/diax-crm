using Diax.Domain.Common;

namespace Diax.Domain.Snippets;

public class Snippet : Entity
{
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // EF Core
    private Snippet() { }

    public Snippet(
        string title,
        string content,
        string language,
        bool isPublic,
        Guid createdByUserId,
        DateTime? expiresAt = null)
    {
        SetTitle(title);
        SetContent(content);
        SetLanguage(language);
        SetVisibility(isPublic);
        SetExpiration(expiresAt);
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
    }

    public void SetContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.", nameof(content));

        Content = content;
    }

    public void SetLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language is required.", nameof(language));

        Language = language.Trim();
    }

    public void SetVisibility(bool isPublic) => IsPublic = isPublic;

    public void SetExpiration(DateTime? expiresAt)
    {
        if (expiresAt.HasValue)
        {
            var value = expiresAt.Value;
            ExpiresAt = value.Kind == DateTimeKind.Utc
                ? value
                : value.Kind == DateTimeKind.Local
                    ? value.ToUniversalTime()
                    : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        else
        {
            ExpiresAt = null;
        }
    }
}
