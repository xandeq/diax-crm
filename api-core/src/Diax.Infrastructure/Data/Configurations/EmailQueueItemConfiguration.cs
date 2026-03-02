using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class EmailQueueItemConfiguration : IEntityTypeConfiguration<EmailQueueItem>
{
    public void Configure(EntityTypeBuilder<EmailQueueItem> builder)
    {
        builder.ToTable("email_queue_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.UserId)
            .IsRequired();

        builder.Property(item => item.RecipientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.RecipientEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(item => item.Subject)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasOne<EmailCampaign>()
               .WithMany()
               .HasForeignKey(i => i.CampaignId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.Property(item => item.HtmlBody)
            .IsRequired()
            .HasMaxLength(50000);

        builder.Property(item => item.AttachmentsJson)
            .HasMaxLength(2000000);

        builder.Property(item => item.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(EmailQueueStatus.Queued);

        builder.Property(item => item.ScheduledAt)
            .IsRequired();

        builder.Property(item => item.ProcessingStartedAt);

        builder.Property(item => item.SentAt);

        builder.Property(item => item.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(item => item.LastError)
            .HasMaxLength(2000);

        builder.Property(item => item.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(item => item.CreatedBy)
            .HasMaxLength(100);

        builder.Property(item => item.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(item => item.DeliveredAt);

        builder.Property(item => item.OpenedAt);

        builder.Property(item => item.ReadCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(item => new { item.Status, item.ScheduledAt })
            .HasDatabaseName("IX_EmailQueueItem_Status_ScheduledAt");

        builder.HasIndex(item => item.SentAt)
            .HasDatabaseName("IX_EmailQueueItem_SentAt");

        builder.HasIndex(item => item.CampaignId)
            .HasDatabaseName("IX_EmailQueueItem_CampaignId");

        builder.HasIndex(item => new { item.UserId, item.CreatedAt })
            .HasDatabaseName("IX_EmailQueueItem_UserId_CreatedAt");
    }
}
