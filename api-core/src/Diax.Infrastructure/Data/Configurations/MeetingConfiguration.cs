using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("meetings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.ContactName).HasColumnName("contact_name").IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").IsRequired().HasMaxLength(320);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(50);
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.ScheduledAt).HasColumnName("scheduled_at").IsRequired();
        builder.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);

        // Anti double-booking: um slot Confirmed por usuário/horário
        builder.HasIndex(x => new { x.UserId, x.ScheduledAt })
            .IsUnique()
            .HasDatabaseName("UX_meetings_user_slot")
            .HasFilter("[status] = 0");

        builder.HasIndex(x => x.CustomerId);
    }
}
