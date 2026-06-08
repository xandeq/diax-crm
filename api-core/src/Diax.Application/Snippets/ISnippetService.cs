using Diax.Application.Snippets.Dtos;

namespace Diax.Application.Snippets;

public interface ISnippetService
{
    Task<Guid> CreateAsync(CreateSnippetRequestDto dto, Guid userId, IEnumerable<SnippetAttachmentUploadDto> attachments, CancellationToken cancellationToken = default);
    Task<List<SnippetResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SnippetResponseDto?> GetByIdAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Guid> AddAttachmentAsync(Guid snippetId, Guid userId, SnippetAttachmentUploadDto attachment, CancellationToken cancellationToken = default);
    Task<(string StoredFileName, string OriginalFileName, string ContentType, Guid OwnerUserId)?> GetAttachmentDownloadInfoAsync(Guid snippetId, Guid attachmentId, Guid? userId, CancellationToken cancellationToken = default);
    Task<string?> DeleteAttachmentAsync(Guid snippetId, Guid attachmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetAttachmentStoredNamesAsync(Guid snippetId, Guid userId, CancellationToken cancellationToken = default);
}
