using Diax.Application.Blog.Dtos;
using Diax.Application.Blog.Services;
using Diax.Application.Common;
using Diax.Domain.Blog;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Blog;

public class BlogPostService : IApplicationService
{
    private readonly IBlogPostRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizerService _htmlSanitizer;
    private readonly ISlugGeneratorService _slugGenerator;
    private readonly ILogger<BlogPostService> _logger;

    public BlogPostService(
        IBlogPostRepository repository,
        IUnitOfWork unitOfWork,
        IHtmlSanitizerService htmlSanitizer,
        ISlugGeneratorService slugGenerator,
        ILogger<BlogPostService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _slugGenerator = slugGenerator;
        _logger = logger;
    }

    // ===== MÉTODOS ADMIN =====

    public async Task<Result<BlogPostResponse>> CreateAsync(
        CreateBlogPostRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Criando novo post: {Title}", request.Title);

            // Gerar slug se não fornecido
            var slug = string.IsNullOrWhiteSpace(request.Slug)
                ? await _slugGenerator.GenerateUniqueSlugAsync(request.Title, cancellationToken: cancellationToken)
                : request.Slug;

            // Verificar duplicação de slug
            if (await _repository.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            {
                return Result.Failure<BlogPostResponse>(
                    Error.Conflict("Slug", "Slug já está em uso."));
            }

            // Sanitizar HTML
            var sanitizedContent = _htmlSanitizer.Sanitize(request.ContentHtml);

            // Criar post
            var post = BlogPost.Create(
                request.Title,
                slug,
                sanitizedContent,
                request.Excerpt,
                request.AuthorName);

            post.SetCreatedBy(userId);

            // Configurar campos opcionais
            if (!string.IsNullOrWhiteSpace(request.MetaTitle) ||
                !string.IsNullOrWhiteSpace(request.MetaDescription) ||
                !string.IsNullOrWhiteSpace(request.Keywords))
            {
                post.UpdateSeo(request.MetaTitle, request.MetaDescription, request.Keywords);
            }

            if (!string.IsNullOrWhiteSpace(request.FeaturedImageUrl))
            {
                post.SetFeaturedImage(request.FeaturedImageUrl);
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                post.SetCategory(request.Category);
            }

            if (!string.IsNullOrWhiteSpace(request.Tags))
            {
                post.SetTags(request.Tags);
            }

            // Publicar imediatamente se solicitado
            if (request.PublishImmediately)
            {
                post.Publish(userId);
            }

            await _repository.AddAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post criado com sucesso: {Id}", post.Id);

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar post: {Title}", request.Title);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.CreateFailed", "Falha ao criar post."));
        }
    }

    public async Task<Result<BlogPostResponse>> UpdateAsync(
        Guid id,
        UpdateBlogPostRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetByIdAsync(id, cancellationToken);
            if (post == null)
            {
                return Result.Failure<BlogPostResponse>(
                    Error.NotFound("BlogPost", id));
            }

            // Sanitizar HTML
            var sanitizedContent = _htmlSanitizer.Sanitize(request.ContentHtml);

            // Atualizar conteúdo
            post.UpdateContent(request.Title, sanitizedContent, request.Excerpt);

            // Atualizar slug se fornecido e diferente
            if (!string.IsNullOrWhiteSpace(request.Slug) && request.Slug != post.Slug)
            {
                // Verificar se novo slug já existe
                if (await _repository.SlugExistsAsync(request.Slug, id, cancellationToken))
                {
                    return Result.Failure<BlogPostResponse>(
                        Error.Conflict("Slug", "Slug já está em uso."));
                }
                post.UpdateSlug(request.Slug);
            }

            // Atualizar SEO
            post.UpdateSeo(request.MetaTitle, request.MetaDescription, request.Keywords);

            // Atualizar campos opcionais
            post.SetFeaturedImage(request.FeaturedImageUrl);
            post.SetCategory(request.Category);
            post.SetTags(request.Tags);

            post.SetUpdated(userId);

            await _repository.UpdateAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post atualizado: {Id}", id);

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar post: {Id}", id);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.UpdateFailed", "Falha ao atualizar post."));
        }
    }

    public async Task<Result<BlogPostResponse>> PublishAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetByIdAsync(id, cancellationToken);
            if (post == null)
            {
                return Result.Failure<BlogPostResponse>(
                    Error.NotFound("BlogPost", id));
            }

            post.Publish(userId);

            await _repository.UpdateAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post publicado: {Id}", id);

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar post: {Id}", id);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.PublishFailed", "Falha ao publicar post."));
        }
    }

    public async Task<Result<BlogPostResponse>> ArchiveAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetByIdAsync(id, cancellationToken);
            if (post == null)
            {
                return Result.Failure<BlogPostResponse>(
                    Error.NotFound("BlogPost", id));
            }

            post.Archive();
            post.SetUpdated(userId);

            await _repository.UpdateAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post arquivado: {Id}", id);

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao arquivar post: {Id}", id);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.ArchiveFailed", "Falha ao arquivar post."));
        }
    }

    public async Task<Result<BlogPostResponse>> ToggleFeaturedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetByIdAsync(id, cancellationToken);
            if (post == null)
            {
                return Result.Failure<BlogPostResponse>(
                    Error.NotFound("BlogPost", id));
            }

            if (post.IsFeatured)
                post.Unfeature();
            else
                post.Feature();

            await _repository.UpdateAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post featured toggled: {Id}", id);

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar featured do post: {Id}", id);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.ToggleFeaturedFailed", "Falha ao alternar destaque."));
        }
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetByIdAsync(id, cancellationToken);
            if (post == null)
            {
                return Result.Failure(
                    Error.NotFound("BlogPost", id));
            }

            await _repository.DeleteAsync(post, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post deletado: {Id}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar post: {Id}", id);
            return Result.Failure(
                new Error("BlogPost.DeleteFailed", "Falha ao deletar post."));
        }
    }

    // ===== MÉTODOS PÚBLICOS =====

    public async Task<Result<BlogPostResponse>> GetBySlugAsync(
        string slug,
        bool incrementViews,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetBySlugAsync(slug, cancellationToken);
            if (post == null)
            {
                return Result.Failure<BlogPostResponse>(
                    Error.NotFound("BlogPost", slug));
            }

            if (incrementViews)
            {
                post.IncrementViews();
                await _repository.UpdateAsync(post, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter post por slug: {Slug}", slug);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.GetBySlugFailed", "Falha ao obter post."));
        }
    }

    public async Task<Result<PagedResponse<BlogPostResponse>>> GetPublishedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _repository.GetPublishedAsync(page, pageSize, cancellationToken);

            var response = new PagedResponse<BlogPostResponse>
            {
                Items = result.Items.Select(BlogPostResponse.FromEntity),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter posts publicados");
            return Result.Failure<PagedResponse<BlogPostResponse>>(
                new Error("BlogPost.GetPublishedFailed", "Falha ao obter posts publicados."));
        }
    }

    public async Task<Result<IEnumerable<BlogPostResponse>>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var posts = await _repository.GetFeaturedPostsAsync(count, cancellationToken);
            var responses = posts.Select(BlogPostResponse.FromEntity);

            return Result.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter posts em destaque");
            return Result.Failure<IEnumerable<BlogPostResponse>>(
                new Error("BlogPost.GetFeaturedFailed", "Falha ao obter posts em destaque."));
        }
    }

    // ===== MÉTODOS ADMIN (LISTAGEM) =====

    public async Task<Result<PagedResponse<BlogPostResponse>>> GetPagedAsync(
        BlogPostListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _repository.GetPagedAsync(
                request.Page,
                request.PageSize,
                request.Search,
                request.Status,
                request.Category,
                cancellationToken);

            var response = new PagedResponse<BlogPostResponse>
            {
                Items = result.Items.Select(BlogPostResponse.FromEntity),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter posts paginados");
            return Result.Failure<PagedResponse<BlogPostResponse>>(
                new Error("BlogPost.GetPagedFailed", "Falha ao obter posts."));
        }
    }

    public async Task<Result<BlogPostResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _repository.GetByIdAsync(id, cancellationToken);
            if (post == null)
            {
                return Result.Failure<BlogPostResponse>(
                    Error.NotFound("BlogPost", id));
            }

            return Result.Success(BlogPostResponse.FromEntity(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter post: {Id}", id);
            return Result.Failure<BlogPostResponse>(
                new Error("BlogPost.GetByIdFailed", "Falha ao obter post."));
        }
    }
}

public record PagedResponse<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
