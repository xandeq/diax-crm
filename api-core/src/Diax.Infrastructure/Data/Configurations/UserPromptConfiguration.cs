using Diax.Domain.PromptGenerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class UserPromptConfiguration : IEntityTypeConfiguration<UserPrompt>
{
    public void Configure(EntityTypeBuilder<UserPrompt> builder)
    {
        builder.ToTable("user_prompts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.OriginalInput)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.GeneratedPrompt)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PromptType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Índices para consultas eficientes e isolamento de usuário
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_user_prompts_user_id");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_user_prompts_user_id_created_at")
            .IsDescending(false, true); // UserId ASC, CreatedAt DESC
    }
}
