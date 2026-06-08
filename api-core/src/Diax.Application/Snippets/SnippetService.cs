using System.Text.RegularExpressions;
using Diax.Application.Common;
using Diax.Application.Snippets.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Snippets;

namespace Diax.Application.Snippets;

public class SnippetService : IApplicationService, ISnippetService
{
    public const int MaxTitleLength = 200;
    public const int MaxContentLength = 10000;
    public const int MaxLanguageLength = 50;

    private static readonly Regex ScriptTagRegex = new("<script[\\s\\S]*?>[\\s\\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);

    private readonly ISnippetRepository _repository;
    private readonly ISnippetAttachmentRepository _attachmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SnippetService(
        ISnippetRepository repository,
        ISnippetAttachmentRepository attachmentRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _attachmentRepository = attachmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(
        CreateSnippetRequestDto dto,
        Guid userId,
        IEnumerable<SnippetAttachmentUploadDto> attachments,
        CancellationToken cancellationToken = default)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var title = SanitizeText(dto.Title);
        var content = SanitizeContent(dto.Content);
        var language = SanitizeText(dto.Language);
        var attachmentList = attachments?.ToList() ?? new List<SnippetAttachmentUploadDto>();

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(dto.Title));
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Linguagem é obrigatória.", nameof(dto.Language));

        if (string.IsNullOrWhiteSpace(content) && attachmentList.Count == 0)
            throw new ArgumentException("Informe um texto ou ao menos um arquivo.");

        if (title.Length > MaxTitleLength)
            throw new ArgumentException($"Título excede {MaxTitleLength} caracteres.", nameof(dto.Title));
        if (!string.IsNullOrWhiteSpace(content) && content.Length > MaxContentLength)
            throw new ArgumentException($"Conteúdo excede {MaxContentLength} caracteres.", nameof(dto.Content));
        if (language.Length > MaxLanguageLength)
            throw new ArgumentException($"Linguagem excede {MaxLanguageLength} caracteres.", nameof(dto.Language));

        if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value <= DateTime.UtcNow)
            throw new ArgumentException("Data de expiração precisa ser futura.", nameof(dto.ExpiresAt));

        var snippet = new Snippet(
            title,
            content,
            language,
            dto.IsPublic,
            userId,
            dto.ExpiresAt);

        await _repository.AddAsync(snippet, cancellationToken);

        foreach (var att in attachmentList)
        {
            var attachment = new SnippetAttachment(snippet.Id, att.OriginalFileName, att.StoredFileName, att.ContentType, att.SizeBytes);
            await _attachmentRepository.AddAsync(attachment, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return snippet.Id;
    }

    public async Task<List<SnippetResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var snippets = await _repository.GetByUserIdWithAttachmentsAsync(userId, cancellationToken);

        return snippets
            .Where(s => !s.ExpiresAt.HasValue || s.ExpiresAt.Value > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<SnippetResponseDto?> GetByIdAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        Snippet? snippet;

        if (userId.HasValue)
        {
            snippet = await _repository.GetByIdWithAttachmentsAsync(id, cancellationToken);

            if (snippet is null)
                return null;

            if (!snippet.IsPublic && snippet.CreatedByUserId != userId.Value)
                return null;
        }
        else
        {
            snippet = await _repository.GetPublicByIdWithAttachmentsAsync(id, cancellationToken);
        }

        if (snippet is null)
            return null;

        if (snippet.ExpiresAt.HasValue && snippet.ExpiresAt.Value <= now)
            return null;

        return MapToResponse(snippet);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var snippet = await _repository.GetByIdAsync(id, cancellationToken);

        if (snippet is null)
            throw new KeyNotFoundException("Snippet não encontrado.");

        if (snippet.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para deletar este snippet.");

        await _repository.DeleteAsync(snippet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddAttachmentAsync(
        Guid snippetId,
        Guid userId,
        SnippetAttachmentUploadDto att,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _repository.GetByIdAsync(snippetId, cancellationToken);

        if (snippet is null)
            throw new KeyNotFoundException("Snippet não encontrado.");

        if (snippet.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para modificar este snippet.");

        var attachment = new SnippetAttachment(snippetId, att.OriginalFileName, att.StoredFileName, att.ContentType, att.SizeBytes);
        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attachment.Id;
    }

    public async Task<(string StoredFileName, string OriginalFileName, string ContentType, Guid OwnerUserId)?> GetAttachmentDownloadInfoAsync(
        Guid snippetId,
        Guid attachmentId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);

        if (attachment is null || attachment.SnippetId != snippetId)
            return null;

        var snippet = await _repository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null)
            return null;

        if (snippet.ExpiresAt.HasValue && snippet.ExpiresAt.Value <= now)
            return null;

        if (userId.HasValue)
        {
            if (snippet.CreatedByUserId != userId.Value && !snippet.IsPublic)
                return null;
        }
        else
        {
            if (!snippet.IsPublic)
                return null;
        }

        return (attachment.StoredFileName, attachment.OriginalFileName, attachment.ContentType, snippet.CreatedByUserId);
    }

    public async Task<string?> DeleteAttachmentAsync(
        Guid snippetId,
        Guid attachmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);

        if (attachment is null || attachment.SnippetId != snippetId)
            throw new KeyNotFoundException("Anexo não encontrado.");

        var snippet = await _repository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null || snippet.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para remover este anexo.");

        var storedFileName = attachment.StoredFileName;

        await _attachmentRepository.DeleteAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storedFileName;
    }

    public async Task<List<string>> GetAttachmentStoredNamesAsync(Guid snippetId, Guid userId, CancellationToken cancellationToken = default)
    {
        var attachments = await _attachmentRepository.GetBySnippetIdAsync(snippetId, cancellationToken);
        // Return only for the owner
        var snippet = await _repository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null || snippet.CreatedByUserId != userId)
            return new List<string>();

        return attachments.Select(a => a.StoredFileName).ToList();
    }

    private static SnippetResponseDto MapToResponse(Snippet snippet) => new()
    {
        Id = snippet.Id,
        Title = snippet.Title,
        Content = snippet.Content,
        Language = snippet.Language,
        IsPublic = snippet.IsPublic,
        CreatedAt = snippet.CreatedAt,
        Attachments = snippet.Attachments.Select(a => new SnippetAttachmentDto
        {
            Id = a.Id,
            OriginalFileName = a.OriginalFileName,
            ContentType = a.ContentType,
            SizeBytes = a.SizeBytes,
            CreatedAt = a.CreatedAt
        }).ToList()
    };

    private static string SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }

    private static string SanitizeContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var withoutScripts = ScriptTagRegex.Replace(value, string.Empty);
        var withoutTags = HtmlTagRegex.Replace(withoutScripts, string.Empty);
        return withoutTags.Trim();
    }
}
