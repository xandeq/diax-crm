namespace Diax.Application.EmailMarketing;

public class EmailSendMessage
{
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public List<EmailSendAttachment> Attachments { get; set; } = [];
}

public class EmailSendAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string Base64Content { get; set; } = string.Empty;
}

public class EmailSendResult
{
    public bool Success { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public static EmailSendResult Ok(string? providerMessageId = null)
    {
        return new EmailSendResult { Success = true, ProviderMessageId = providerMessageId };
    }

    public static EmailSendResult Fail(string errorMessage)
    {
        return new EmailSendResult { Success = false, ErrorMessage = errorMessage };
    }
}
