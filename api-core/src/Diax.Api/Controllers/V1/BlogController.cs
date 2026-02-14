using Asp.Versioning;
using Diax.Application.Blog;
using Diax.Application.Blog.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class BlogController : BaseApiController
{
    private readonly BlogPostService _blogPostService;
    private readonly ILogger<BlogController> _logger;

    public BlogController(
        BlogPostService blogPostService,
        ILogger<BlogController> logger)
    {
        _blogPostService = blogPostService;
        _logger = logger;
    }

    // ===== ENDPOINTS PÚBLICOS (Sem autenticação) =====

    /// <summary>
    /// Obtém artigos publicados (público)
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<BlogPostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Obtendo posts publicados - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        var result = await _blogPostService.GetPublishedAsync(page, pageSize, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém artigo por slug (público, incrementa views)
    /// </summary>
    [HttpGet("public/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Obtendo post por slug: {Slug}", slug);
        var result = await _blogPostService.GetBySlugAsync(slug, incrementViews: true, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém artigos em destaque (público)
    /// </summary>
    [HttpGet("public/featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<BlogPostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedPosts(
        [FromQuery] int count = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Obtendo posts em destaque - Count: {Count}", count);
        var result = await _blogPostService.GetFeaturedAsync(count, cancellationToken);
        return HandleResult(result);
    }

    // ===== ENDPOINTS ADMIN (Requerem autenticação JWT ou API Key) =====

    /// <summary>
    /// Lista todos os posts (admin com filtros)
    /// </summary>
    [HttpGet("admin")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<BlogPostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPosts(
        [FromQuery] BlogPostListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listando posts admin - Page: {Page}, Status: {Status}", request.Page, request.Status);
        var result = await _blogPostService.GetPagedAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém post por ID (admin)
    /// </summary>
    [HttpGet("admin/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Obtendo post por ID: {Id}", id);
        var result = await _blogPostService.GetByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cria novo post (admin ou N8N)
    /// </summary>
    [HttpPost("admin")]
    [Authorize]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreateBlogPostRequest request,
        [FromServices] DiaxDbContext db,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Criando novo post: {Title}", request.Title);

        var userId = await ResolveUserIdAsync(db, cancellationToken);
        var userIdString = userId?.ToString() ?? "system";

        var result = await _blogPostService.CreateAsync(request, userIdString, cancellationToken);

        if (result.IsFailure)
            return HandleResult(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Atualiza post existente (admin)
    /// </summary>
    [HttpPut("admin/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(
        Guid id,
        [FromBody] UpdateBlogPostRequest request,
        [FromServices] DiaxDbContext db,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Atualizando post: {Id}", id);

        var userId = await ResolveUserIdAsync(db, cancellationToken);
        var userIdString = userId?.ToString() ?? "system";

        var result = await _blogPostService.UpdateAsync(id, request, userIdString, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Publica post (muda status para Published)
    /// </summary>
    [HttpPatch("admin/{id}/publish")]
    [Authorize]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishPost(
        Guid id,
        [FromServices] DiaxDbContext db,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publicando post: {Id}", id);

        var userId = await ResolveUserIdAsync(db, cancellationToken);
        var userIdString = userId?.ToString() ?? "system";

        var result = await _blogPostService.PublishAsync(id, userIdString, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Arquiva post (muda status para Archived)
    /// </summary>
    [HttpPatch("admin/{id}/archive")]
    [Authorize]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchivePost(
        Guid id,
        [FromServices] DiaxDbContext db,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Arquivando post: {Id}", id);

        var userId = await ResolveUserIdAsync(db, cancellationToken);
        var userIdString = userId?.ToString() ?? "system";

        var result = await _blogPostService.ArchiveAsync(id, userIdString, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Alterna status de destaque (featured)
    /// </summary>
    [HttpPatch("admin/{id}/toggle-featured")]
    [Authorize]
    [ProducesResponseType(typeof(BlogPostResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleFeatured(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Alternando featured do post: {Id}", id);
        var result = await _blogPostService.ToggleFeaturedAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Exclui post permanentemente
    /// </summary>
    [HttpDelete("admin/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deletando post: {Id}", id);
        var result = await _blogPostService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleResult(result);

        return NoContent();
    }
}
