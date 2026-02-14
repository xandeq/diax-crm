namespace Diax.Application.Blog.Dtos;

public record UpdateBlogPostRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string ContentHtml { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? Keywords { get; init; }
    public string? FeaturedImageUrl { get; init; }
    public string? Category { get; init; }
    public string? Tags { get; init; }
}
