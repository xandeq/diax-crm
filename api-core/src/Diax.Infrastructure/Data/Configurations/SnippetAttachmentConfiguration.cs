using Diax.Domain.Snippets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class SnippetAttachmentConfiguration : IEntityTypeConfiguration<SnippetAttachment>
{
    public void Configure(EntityTypeBuilder<SnippetAttachment> builder)
    {
        builder.ToTable("snippet_attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnippetId)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.StoredFileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Snippet)
            .WithMany(s => s.Attachments)
            .HasForeignKey(x => x.SnippetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SnippetId)
            .HasDatabaseName("IX_snippet_attachments_snippet_id");
    }
}
