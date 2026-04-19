using Diax.Domain.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Description)
            .HasMaxLength(512);

        builder.Property(a => a.DurationMinutes)
            .HasDefaultValue(60);

        builder.Property(a => a.IsCancelled)
            .HasDefaultValue(false);

        builder.HasOne(a => a.Label)
            .WithMany()
            .HasForeignKey(a => a.LabelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.RecurrenceGroupId)
            .HasDatabaseName("IX_Appointments_RecurrenceGroupId")
            .HasFilter("[recurrence_group_id] IS NOT NULL");
    }
}
