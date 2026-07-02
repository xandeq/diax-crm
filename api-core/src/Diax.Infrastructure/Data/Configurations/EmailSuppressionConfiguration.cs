using Diax.Domain.EmailMarketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class EmailSuppressionConfiguration : IEntityTypeConfiguration<EmailSuppression>
{
    public void Configure(EntityTypeBuilder<EmailSuppression> builder)
    {
        builder.ToTable("email_suppressions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.DomainPattern).HasMaxLength(255);
        builder.Property(x => x.Source).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Reason).IsRequired().HasConversion<int>();

        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        // IsSuppressedAsync roda POR DESTINATÁRIO no loop de enfileiramento —
        // sem índice era um scan por lead. Hot path de compliance.
        builder.HasIndex(x => new { x.UserId, x.Email })
            .HasDatabaseName("IX_EmailSuppression_User_Email");

        builder.HasIndex(x => new { x.UserId, x.DomainPattern })
            .HasDatabaseName("IX_EmailSuppression_User_DomainPattern");
    }
}
