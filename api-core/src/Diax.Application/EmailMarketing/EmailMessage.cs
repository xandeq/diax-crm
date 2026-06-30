namespace Diax.Application.EmailMarketing;

/// <summary>
/// Mensagem enriquecida para envio via IEmailDispatchService.
/// Não modifica EmailSendMessage — é um tipo paralelo para o novo endpoint.
/// </summary>
public sealed class EmailMessage
{
    public required EmailAddress From { get; init; }
    public required IReadOnlyList<EmailAddress> To { get; init; }
    public IReadOnlyList<EmailAddress>? Cc { get; init; }
    public IReadOnlyList<EmailAddress>? Bcc { get; init; }
    public EmailAddress? ReplyTo { get; init; }
    public required string Subject { get; init; }
    public required string Html { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<EmailSendAttachment>? Attachments { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
}

public readonly record struct EmailAddress(string Address, string? Display = null);
