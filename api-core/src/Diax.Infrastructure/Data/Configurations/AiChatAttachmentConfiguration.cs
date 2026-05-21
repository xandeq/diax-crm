using Diax.Domain.AiChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AiChatAttachmentConfiguration : IEntityTypeConfiguration<AiChatAttachment>
{
    public void Configure(EntityTypeBuilder<AiChatAttachment> builder)
    {
        builder.ToTable("ai_chat_attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SizeBytes).IsRequired();

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.MessageId)
            .HasDatabaseName("IX_ai_chat_attachments_message_id");
    }
}
