using Diax.Application.Snippets.Dtos;

namespace Diax.Application.Snippets;

public interface ISnippetService
{
    Task<Guid> CreateAsync(CreateSnippetRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<List<SnippetResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SnippetResponseDto?> GetByIdAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
