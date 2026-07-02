using Diax.Domain.EmailMarketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class EmailEventConfiguration : IEntityTypeConfiguration<EmailEvent>
{
    public void Configure(EntityTypeBuilder<EmailEvent> builder)
    {
        builder.ToTable("email_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(x => x.Metadata)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        // Ledger de idempotência dos webhooks: no máximo UM evento por (item, tipo).
        builder.HasIndex(x => new { x.QueueItemId, x.EventType })
            .IsUnique()
            .HasFilter("[queue_item_id] IS NOT NULL")
            .HasDatabaseName("UX_EmailEvent_QueueItem_EventType");

        builder.HasIndex(x => new { x.CampaignId, x.EventType })
            .HasDatabaseName("IX_EmailEvent_Campaign_EventType");

        builder.HasIndex(x => x.OccurredAt)
            .HasDatabaseName("IX_EmailEvent_OccurredAt");
    }
}
