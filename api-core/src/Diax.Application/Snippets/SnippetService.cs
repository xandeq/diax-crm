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
    private readonly IUnitOfWork _unitOfWork;

    public SnippetService(ISnippetRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateSnippetRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var title = SanitizeText(dto.Title);
        var content = SanitizeContent(dto.Content);
        var language = SanitizeText(dto.Language);

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(dto.Title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Conteúdo é obrigatório.", nameof(dto.Content));
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Linguagem é obrigatória.", nameof(dto.Language));

        if (title.Length > MaxTitleLength)
            throw new ArgumentException($"Título excede {MaxTitleLength} caracteres.", nameof(dto.Title));
        if (content.Length > MaxContentLength)
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return snippet.Id;
    }

    public async Task<List<SnippetResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var snippets = await _repository.GetByUserIdAsync(userId, cancellationToken);

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
            snippet = await _repository.GetByIdAsync(id, cancellationToken);

            if (snippet is null)
                return null;

            if (!snippet.IsPublic && snippet.CreatedByUserId != userId.Value)
                return null;
        }
        else
        {
            snippet = await _repository.GetPublicByIdAsync(id, cancellationToken);
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

    private static SnippetResponseDto MapToResponse(Snippet snippet) => new()
    {
        Id = snippet.Id,
        Title = snippet.Title,
        Content = snippet.Content,
        Language = snippet.Language,
        IsPublic = snippet.IsPublic,
        CreatedAt = snippet.CreatedAt
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
