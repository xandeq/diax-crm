using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("proposals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.PublicToken).HasColumnName("public_token").IsRequired().HasMaxLength(64);
        builder.Property(x => x.PixKey).HasColumnName("pix_key").HasMaxLength(100);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until");
        builder.Property(x => x.SentAt).HasColumnName("sent_at");
        builder.Property(x => x.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(x => x.PaidAt).HasColumnName("paid_at");
        builder.Property(x => x.ViewCount).HasColumnName("view_count").IsRequired();
        builder.Property(x => x.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);

        builder.HasIndex(x => x.PublicToken).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
