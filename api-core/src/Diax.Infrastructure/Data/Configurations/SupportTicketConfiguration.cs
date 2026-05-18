using Diax.Domain.Helpdesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CustomerName)
            .HasMaxLength(256);
    }
}
