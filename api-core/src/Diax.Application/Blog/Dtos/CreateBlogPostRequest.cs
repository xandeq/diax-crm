namespace Diax.Application.Blog.Dtos;

public record CreateBlogPostRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; } // Opcional - auto-gera se null/empty
    public string ContentHtml { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? Keywords { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? FeaturedImageUrl { get; init; }
    public string? Category { get; init; }
    public string? Tags { get; init; }
    public bool PublishImmediately { get; init; } = false;
}
