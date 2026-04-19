using Diax.Domain.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AppointmentLabelConfiguration : IEntityTypeConfiguration<AppointmentLabel>
{
    public void Configure(EntityTypeBuilder<AppointmentLabel> builder)
    {
        builder.ToTable("appointment_labels");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Color)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.Order)
            .HasDefaultValue(0);

        builder.HasIndex(l => l.UserId)
            .HasDatabaseName("IX_AppointmentLabels_UserId");
    }
}
