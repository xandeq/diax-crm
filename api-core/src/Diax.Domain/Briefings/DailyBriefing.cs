using Diax.Domain.Common;

namespace Diax.Domain.Briefings;

/// <summary>
/// Briefing diário gerado por um dos agentes (routines/workflow) e ingerido via API.
/// A área "Daily Briefings" do CRM mostra apenas os briefings do dia corrente.
/// </summary>
public class DailyBriefing : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }

    /// <summary>Dia do briefing (calendário America/São_Paulo). DateOnly evita shift de UTC.</summary>
    public DateOnly BriefingDate { get; private set; }

    /// <summary>Slug do gerador: claude-ai | codex-chatgpt | tarefas-ias.</summary>
    public string Source { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>Conteúdo completo (HTML ou Markdown). nvarchar(max).</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>html | markdown.</summary>
    public string ContentFormat { get; private set; } = "html";

    // EF Core
    private DailyBriefing() { }

    public DailyBriefing(Guid userId, DateOnly briefingDate, string source, string title, string content, string? contentFormat)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
        BriefingDate = briefingDate;
        SetSource(source);
        SetContent(title, content, contentFormat);
    }

    public void SetContent(string title, string content, string? contentFormat)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
        Content = content ?? string.Empty;
        ContentFormat = NormalizeFormat(contentFormat);
    }

    private void SetSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));

        Source = source.Trim().ToLowerInvariant();
    }

    private static string NormalizeFormat(string? format)
    {
        var f = (format ?? "html").Trim().ToLowerInvariant();
        return f is "markdown" or "md" ? "markdown" : "html";
    }
}
