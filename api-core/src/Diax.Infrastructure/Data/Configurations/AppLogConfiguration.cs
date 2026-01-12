using Diax.Domain.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AppLogConfiguration : IEntityTypeConfiguration<AppLog>
{
    public void Configure(EntityTypeBuilder<AppLog> builder)
    {
        builder.ToTable("app_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TimestampUtc)
            .IsRequired();

        builder.Property(x => x.Level)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.MessageTemplate)
            .HasMaxLength(2000);

        builder.Property(x => x.Source)
            .HasMaxLength(500);

        builder.Property(x => x.RequestId)
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.UserId)
            .HasMaxLength(100);

        builder.Property(x => x.UserName)
            .HasMaxLength(256);

        builder.Property(x => x.RequestPath)
            .HasMaxLength(2048);

        builder.Property(x => x.QueryString)
            .HasMaxLength(4000);

        builder.Property(x => x.HttpMethod)
            .HasMaxLength(20);

        builder.Property(x => x.HeadersJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ClientIp)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        builder.Property(x => x.ExceptionType)
            .HasMaxLength(500);

        builder.Property(x => x.ExceptionMessage)
            .HasMaxLength(4000);

        builder.Property(x => x.StackTrace)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.InnerException)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.TargetSite)
            .HasMaxLength(512);

        builder.Property(x => x.MachineName)
            .HasMaxLength(128);

        builder.Property(x => x.Environment)
            .HasMaxLength(64);

        builder.Property(x => x.AdditionalData)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(x => x.TimestampUtc)
            .IsDescending()
            .HasDatabaseName("IX_app_logs_timestamp_utc");

        builder.HasIndex(x => new { x.Level, x.TimestampUtc })
            .HasDatabaseName("IX_app_logs_level_timestamp");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_app_logs_correlation_id");

        builder.HasIndex(x => x.RequestId)
            .HasDatabaseName("IX_app_logs_request_id");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_app_logs_user_id");
    }
}
