using Diax.Domain.Ads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class FacebookAdAccountConfiguration : IEntityTypeConfiguration<FacebookAdAccount>
{
    public void Configure(EntityTypeBuilder<FacebookAdAccount> builder)
    {
        builder.Property(e => e.AdAccountId)
            .HasMaxLength(64)
            .IsRequired();

        // Facebook access tokens can be up to 500+ chars
        builder.Property(e => e.AccessToken)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(e => e.AccountName)
            .HasMaxLength(256);

        builder.Property(e => e.Currency)
            .HasMaxLength(10);

        builder.Property(e => e.Timezone)
            .HasMaxLength(64);

        builder.Property(e => e.AccountStatus)
            .HasMaxLength(32);
    }
}
