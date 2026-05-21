using Diax.Domain.AiChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AiChatMessageConfiguration : IEntityTypeConfiguration<AiChatMessage>
{
    public void Configure(EntityTypeBuilder<AiChatMessage> builder)
    {
        builder.ToTable("ai_chat_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConversationId)
            .IsRequired();

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.InputTokens).IsRequired();
        builder.Property(x => x.OutputTokens).IsRequired();
        builder.Property(x => x.CacheReadTokens).IsRequired();
        builder.Property(x => x.CacheCreationTokens).IsRequired();

        // CostUsd: precisão fina (até $9999.999999)
        builder.Property(x => x.CostUsd)
            .HasPrecision(10, 6)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt })
            .HasDatabaseName("IX_ai_chat_messages_conversation_id_created_at");

        // Relacionamento mensagem -> anexos (cascata)
        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(AiChatMessage.Attachments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
