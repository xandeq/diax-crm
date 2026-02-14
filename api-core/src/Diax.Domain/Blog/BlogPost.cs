using Diax.Domain.Common;

namespace Diax.Domain.Blog;

/// <summary>
/// Representa um artigo do blog.
/// </summary>
public class BlogPost : AuditableEntity
{
    // ===== IDENTIFICAÇÃO E CONTEÚDO =====

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ContentHtml { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;

    // ===== SEO =====

    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? Keywords { get; private set; }

    // ===== PUBLICAÇÃO =====

    public BlogPostStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;

    // ===== MÍDIA E ENGAJAMENTO =====

    public string? FeaturedImageUrl { get; private set; }
    public int ViewCount { get; private set; }
    public bool IsFeatured { get; private set; }

    // ===== METADADOS =====

    public string? Category { get; private set; }
    public string? Tags { get; private set; } // Comma-separated

    // Construtor privado para EF Core
    private BlogPost() : base() { }

    /// <summary>
    /// Cria um novo post de blog (status Draft por padrão).
    /// </summary>
    public static BlogPost Create(
        string title,
        string slug,
        string contentHtml,
        string excerpt,
        string authorName)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug é obrigatório.", nameof(slug));

        if (string.IsNullOrWhiteSpace(contentHtml))
            throw new ArgumentException("Conteúdo é obrigatório.", nameof(contentHtml));

        if (string.IsNullOrWhiteSpace(excerpt))
            throw new ArgumentException("Resumo é obrigatório.", nameof(excerpt));

        if (string.IsNullOrWhiteSpace(authorName))
            throw new ArgumentException("Nome do autor é obrigatório.", nameof(authorName));

        return new BlogPost
        {
            Title = title,
            Slug = slug,
            ContentHtml = contentHtml,
            Excerpt = excerpt,
            AuthorName = authorName,
            Status = BlogPostStatus.Draft,
            ViewCount = 0,
            IsFeatured = false
        };
    }

    /// <summary>
    /// Atualiza o conteúdo do post.
    /// </summary>
    public void UpdateContent(string title, string contentHtml, string excerpt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));

        if (string.IsNullOrWhiteSpace(contentHtml))
            throw new ArgumentException("Conteúdo é obrigatório.", nameof(contentHtml));

        if (string.IsNullOrWhiteSpace(excerpt))
            throw new ArgumentException("Resumo é obrigatório.", nameof(excerpt));

        Title = title;
        ContentHtml = contentHtml;
        Excerpt = excerpt;
    }

    /// <summary>
    /// Atualiza os metadados de SEO.
    /// </summary>
    public void UpdateSeo(string? metaTitle, string? metaDescription, string? keywords)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        Keywords = keywords;
    }

    /// <summary>
    /// Publica o post.
    /// </summary>
    public void Publish(string publishedBy)
    {
        if (Status == BlogPostStatus.Published)
            return;

        Status = BlogPostStatus.Published;
        PublishedAt = DateTime.UtcNow;
        SetUpdated(publishedBy);
    }

    /// <summary>
    /// Arquiva o post.
    /// </summary>
    public void Archive()
    {
        Status = BlogPostStatus.Archived;
    }

    /// <summary>
    /// Coloca o post em destaque.
    /// </summary>
    public void Feature()
    {
        IsFeatured = true;
    }

    /// <summary>
    /// Remove o post do destaque.
    /// </summary>
    public void Unfeature()
    {
        IsFeatured = false;
    }

    /// <summary>
    /// Incrementa contador de visualizações.
    /// </summary>
    public void IncrementViews()
    {
        ViewCount++;
    }

    /// <summary>
    /// Define a categoria do post.
    /// </summary>
    public void SetCategory(string? category)
    {
        Category = category;
    }

    /// <summary>
    /// Define as tags do post.
    /// </summary>
    public void SetTags(string? tags)
    {
        Tags = tags;
    }

    /// <summary>
    /// Define a imagem de destaque.
    /// </summary>
    public void SetFeaturedImage(string? imageUrl)
    {
        FeaturedImageUrl = imageUrl;
    }

    /// <summary>
    /// Atualiza o slug.
    /// </summary>
    public void UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug é obrigatório.", nameof(slug));

        Slug = slug;
    }
}
