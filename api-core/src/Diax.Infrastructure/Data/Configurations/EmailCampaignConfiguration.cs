using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class EmailCampaignConfiguration : IEntityTypeConfiguration<EmailCampaign>
{
    public void Configure(EntityTypeBuilder<EmailCampaign> builder)
    {
        builder.ToTable("email_campaigns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Subject)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(c => c.BodyHtml)
            .IsRequired()
            .HasMaxLength(50000);

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.SourceSnippetId);

        builder.Property(c => c.TotalRecipients)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.SentCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.FailedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.OpenCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .HasDefaultValue(EmailCampaignStatus.Draft);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => new { c.UserId, c.CreatedAt });
    }
}
