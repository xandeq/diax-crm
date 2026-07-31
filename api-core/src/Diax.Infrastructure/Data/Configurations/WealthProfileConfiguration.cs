using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class WealthProfileConfiguration : IEntityTypeConfiguration<WealthProfile>
{
    public void Configure(EntityTypeBuilder<WealthProfile> builder)
    {
        builder.ToTable("wealth_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RiskProfile)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.GoalAmount)
            .HasPrecision(18, 2);

        // JSON de alocação-alvo por classe — pode crescer, não limitar a 256
        builder.Property(x => x.TargetAllocationJson)
            .HasColumnType("nvarchar(max)");

        // Um perfil por usuário
        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
