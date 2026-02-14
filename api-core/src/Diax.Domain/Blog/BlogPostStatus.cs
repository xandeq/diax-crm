namespace Diax.Domain.Blog;

/// <summary>
/// Status de publicação de um post do blog.
/// </summary>
public enum BlogPostStatus
{
    /// <summary>
    /// Rascunho (não visível publicamente).
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Publicado (visível publicamente).
    /// </summary>
    Published = 1,

    /// <summary>
    /// Arquivado (não visível, mas preservado).
    /// </summary>
    Archived = 2
}
