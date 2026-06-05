using Diax.Domain.ErrorLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("error_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Environment).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Level).IsRequired().HasConversion<int>();
        builder.Property(x => x.Message).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.ExceptionType).HasMaxLength(500);
        builder.Property(x => x.StackTrace).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Source).HasMaxLength(500);
        builder.Property(x => x.RequestMethod).HasMaxLength(10);
        builder.Property(x => x.RequestPath).HasMaxLength(1000);
        builder.Property(x => x.UserId).HasMaxLength(200);
        builder.Property(x => x.AdditionalData).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Fingerprint).HasMaxLength(16);
        builder.Property(x => x.OccurrenceCount).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.FirstSeenAt).IsRequired();
        builder.Property(x => x.LastSeenAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsResolved).IsRequired();
        builder.Property(x => x.ResolutionNote).HasColumnType("nvarchar(max)");

        // Lista filtrada principal: AppName + Level + IsResolved + OccurredAt
        builder.HasIndex(x => new { x.AppName, x.Level, x.IsResolved, x.OccurredAt })
            .HasDatabaseName("IX_error_logs_app_level_resolved_date");

        // Dedupe por fingerprint (open logs)
        builder.HasIndex(x => new { x.Fingerprint, x.AppName, x.IsResolved })
            .HasDatabaseName("IX_error_logs_fingerprint")
            .HasFilter("[fingerprint] IS NOT NULL");

        // Stats e retenção
        builder.HasIndex(x => new { x.OccurredAt, x.AppName, x.Level })
            .HasDatabaseName("IX_error_logs_occurred_at_app");

        // Retenção (delete batch por data)
        builder.HasIndex(x => x.OccurredAt)
            .HasDatabaseName("IX_error_logs_occurred_at_retention");
    }
}
