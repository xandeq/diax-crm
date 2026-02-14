using Diax.Domain.Blog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        // ===== TABELA =====
        builder.ToTable("blog_posts");

        // ===== CHAVE PRIMÁRIA =====
        builder.HasKey(x => x.Id);

        // ===== IDENTIFICAÇÃO E CONTEÚDO =====
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.ContentHtml)
            .IsRequired();

        builder.Property(x => x.Excerpt)
            .IsRequired()
            .HasMaxLength(500);

        // ===== SEO =====
        builder.Property(x => x.MetaTitle)
            .HasMaxLength(70);

        builder.Property(x => x.MetaDescription)
            .HasMaxLength(160);

        builder.Property(x => x.Keywords)
            .HasMaxLength(500);

        // ===== PUBLICAÇÃO =====
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.PublishedAt);

        builder.Property(x => x.AuthorName)
            .IsRequired()
            .HasMaxLength(100);

        // ===== MÍDIA E ENGAJAMENTO =====
        builder.Property(x => x.FeaturedImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        // ===== METADADOS =====
        builder.Property(x => x.Category)
            .HasMaxLength(100);

        builder.Property(x => x.Tags)
            .HasMaxLength(500);

        // ===== ÍNDICES =====

        // Índice único no slug para busca rápida e garantir unicidade
        builder.HasIndex(x => x.Slug)
            .IsUnique();

        // Índice no status para filtrar posts publicados/rascunhos
        builder.HasIndex(x => x.Status);

        // Índice na data de publicação para ordenação
        builder.HasIndex(x => x.PublishedAt);

        // Índice composto para queries comuns (status + data)
        builder.HasIndex(x => new { x.Status, x.PublishedAt });

        // Índice na categoria para filtros
        builder.HasIndex(x => x.Category);

        // Índice no featured para busca rápida de posts em destaque
        builder.HasIndex(x => x.IsFeatured);

        // Índice no título para busca textual
        builder.HasIndex(x => x.Title);
    }
}
