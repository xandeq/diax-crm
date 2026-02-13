using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AiProviderCredentialConfiguration : IEntityTypeConfiguration<AiProviderCredential>
{
    public void Configure(EntityTypeBuilder<AiProviderCredential> builder)
    {
        builder.ToTable("ai_provider_credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProviderId)
            .IsRequired();

        builder.Property(c => c.ApiKeyEncrypted)
            .IsRequired()
            .HasMaxLength(1000); // Encrypted keys podem ser maiores que plain text

        builder.Property(c => c.ApiKeyLastFourDigits)
            .HasMaxLength(4);

        // Foreign key relationship: cascade delete (se provider for deletado, credenciais também)
        builder.HasOne(c => c.Provider)
            .WithMany()
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: cada provider tem apenas uma credencial
        builder.HasIndex(c => c.ProviderId)
            .IsUnique();

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);
    }
}
