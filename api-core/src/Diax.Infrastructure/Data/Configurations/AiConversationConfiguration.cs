using Diax.Domain.AiChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("ai_conversations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SystemPrompt)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IsArchived)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => new { x.UserId, x.UpdatedAt })
            .HasDatabaseName("IX_ai_conversations_user_id_updated_at")
            .IsDescending(false, true);

        // Relacionamento conversa -> mensagens (cascata)
        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF Core: navegação como readonly collection — usar field
        builder.Metadata.FindNavigation(nameof(AiConversation.Messages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
