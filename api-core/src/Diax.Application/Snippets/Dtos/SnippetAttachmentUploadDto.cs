namespace Diax.Application.Snippets.Dtos;

public record SnippetAttachmentUploadDto(
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes);
