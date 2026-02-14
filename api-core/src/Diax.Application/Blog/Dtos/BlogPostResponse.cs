using Diax.Domain.Blog;

namespace Diax.Application.Blog.Dtos;

public record BlogPostResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ContentHtml { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? Keywords { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusDescription { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? FeaturedImageUrl { get; init; }
    public int ViewCount { get; init; }
    public bool IsFeatured { get; init; }
    public string? Category { get; init; }
    public string[]? TagList { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }

    public static BlogPostResponse FromEntity(BlogPost post)
    {
        return new BlogPostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            ContentHtml = post.ContentHtml,
            Excerpt = post.Excerpt,
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription,
            Keywords = post.Keywords,
            Status = post.Status.ToString(),
            StatusDescription = GetStatusDescription(post.Status),
            PublishedAt = post.PublishedAt,
            AuthorName = post.AuthorName,
            FeaturedImageUrl = post.FeaturedImageUrl,
            ViewCount = post.ViewCount,
            IsFeatured = post.IsFeatured,
            Category = post.Category,
            TagList = ParseTags(post.Tags),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            CreatedBy = post.CreatedBy
        };
    }

    private static string GetStatusDescription(BlogPostStatus status) => status switch
    {
        BlogPostStatus.Draft => "Rascunho",
        BlogPostStatus.Published => "Publicado",
        BlogPostStatus.Archived => "Arquivado",
        _ => "Desconhecido"
    };

    private static string[] ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return Array.Empty<string>();

        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
