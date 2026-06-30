using Diax.Domain.EmailMarketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class EmailSendLogConfiguration : IEntityTypeConfiguration<EmailSendLog>
{
    public void Configure(EntityTypeBuilder<EmailSendLog> builder)
    {
        builder.ToTable("email_send_log");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.ToHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SubjectHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.BodyHash).HasMaxLength(64);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(32);
        builder.Property(x => x.AttemptNo).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.Property(x => x.LatencyMs).IsRequired();
        builder.Property(x => x.ProviderMessageId).HasMaxLength(200);
        builder.Property(x => x.FromDomain).IsRequired().HasMaxLength(255);
        builder.Property(x => x.AllowUnaligned).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.IdempotencyKey)
               .HasDatabaseName("IX_EmailSendLog_IdempotencyKey");
        builder.HasIndex(x => x.RequestId)
               .HasDatabaseName("IX_EmailSendLog_RequestId");
        builder.HasIndex(x => x.CreatedAt)
               .HasDatabaseName("IX_EmailSendLog_CreatedAt");
        builder.HasIndex(x => new { x.IdempotencyKey, x.CreatedAt })
               .HasDatabaseName("IX_EmailSendLog_Idem_CreatedAt");
    }
}
