using Diax.Domain.Snippets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class SnippetConfiguration : IEntityTypeConfiguration<Snippet>
{
    public void Configure(EntityTypeBuilder<Snippet> builder)
    {
        builder.ToTable("snippets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Language)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IsPublic)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.HasIndex(x => x.CreatedByUserId)
            .HasDatabaseName("IX_snippets_created_by_user_id");

        builder.HasIndex(x => x.IsPublic)
            .HasDatabaseName("IX_snippets_is_public");
    }
}
