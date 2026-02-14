using Diax.Domain.Common;
using Diax.Shared.Results;

namespace Diax.Domain.Blog;

/// <summary>
/// Repositório para operações com posts do blog.
/// </summary>
public interface IBlogPostRepository : IRepository<BlogPost>
{
    // ===== QUERIES PÚBLICAS (SITE) =====

    /// <summary>
    /// Obtém um post publicado por slug.
    /// </summary>
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém posts publicados com paginação.
    /// </summary>
    Task<PagedResult<BlogPost>> GetPublishedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém posts em destaque.
    /// </summary>
    Task<IEnumerable<BlogPost>> GetFeaturedPostsAsync(
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém posts por categoria.
    /// </summary>
    Task<PagedResult<BlogPost>> GetByCategoryAsync(
        string category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // ===== QUERIES ADMIN =====

    /// <summary>
    /// Obtém posts com filtros (admin).
    /// </summary>
    Task<PagedResult<BlogPost>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        BlogPostStatus? status = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    // ===== VALIDAÇÕES =====

    /// <summary>
    /// Verifica se um slug já existe.
    /// </summary>
    Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
