using Diax.Domain.Blog;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class BlogPostRepository : Repository<BlogPost>, IBlogPostRepository
{
    public BlogPostRepository(DiaxDbContext context) : base(context)
    {
    }

    // ===== QUERIES PÚBLICAS =====

    public async Task<BlogPost?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                x => x.Slug == slug && x.Status == BlogPostStatus.Published,
                cancellationToken);
    }

    public async Task<PagedResult<BlogPost>> GetPublishedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(x => x.Status == BlogPostStatus.Published)
            .OrderByDescending(x => x.PublishedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BlogPost>(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<BlogPost>> GetFeaturedPostsAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.Status == BlogPostStatus.Published && x.IsFeatured)
            .OrderByDescending(x => x.PublishedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<BlogPost>> GetByCategoryAsync(
        string category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(x => x.Status == BlogPostStatus.Published && x.Category == category)
            .OrderByDescending(x => x.PublishedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BlogPost>(items, totalCount, page, pageSize);
    }

    // ===== QUERIES ADMIN =====

    public async Task<PagedResult<BlogPost>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        BlogPostStatus? status = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        // Filtro de busca textual
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Title.Contains(search) ||
                x.Excerpt.Contains(search) ||
                (x.Category != null && x.Category.Contains(search)));
        }

        // Filtro de status
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        // Filtro de categoria
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        // Ordenação por data de criação (mais recentes primeiro)
        query = query.OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BlogPost>(items, totalCount, page, pageSize);
    }

    // ===== VALIDAÇÕES =====

    public async Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(x => x.Slug == slug);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
