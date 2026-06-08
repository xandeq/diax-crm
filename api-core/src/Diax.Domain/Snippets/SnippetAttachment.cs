using Diax.Domain.Common;

namespace Diax.Domain.Snippets;

public class SnippetAttachment : Entity
{
    public Guid SnippetId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core
    public Snippet? Snippet { get; private set; }

    private SnippetAttachment() { }

    public SnippetAttachment(
        Guid snippetId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long sizeBytes)
    {
        SnippetId = snippetId;
        OriginalFileName = originalFileName.Trim();
        StoredFileName = storedFileName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        CreatedAt = DateTime.UtcNow;
    }
}
