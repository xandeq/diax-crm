using Diax.Domain.Blog;

namespace Diax.Application.Blog.Dtos;

public record BlogPostListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public BlogPostStatus? Status { get; init; }
    public string? Category { get; init; }
}
