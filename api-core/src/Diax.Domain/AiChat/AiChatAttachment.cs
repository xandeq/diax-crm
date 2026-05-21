using Diax.Domain.Common;

namespace Diax.Domain.AiChat;

/// <summary>
/// Arquivo de texto anexado pelo usuário a uma mensagem. Apenas texto — sem binários.
/// Conteúdo do arquivo vira parte do prompt enviado para a Anthropic.
/// </summary>
public class AiChatAttachment : Entity
{
    public Guid MessageId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public int SizeBytes { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // EF Core
    private AiChatAttachment() { }

    public AiChatAttachment(
        Guid messageId,
        string fileName,
        string contentType,
        int sizeBytes,
        string content)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId is required.", nameof(messageId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));
        if (sizeBytes < 0)
            throw new ArgumentException("SizeBytes must be non-negative.", nameof(sizeBytes));

        MessageId = messageId;
        FileName = fileName.Trim().Length > 255 ? fileName.Trim()[..255] : fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType)
            ? "text/plain"
            : contentType.Trim();
        SizeBytes = sizeBytes;
        Content = content ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
