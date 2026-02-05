using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class GroupAiProviderAccessConfiguration : IEntityTypeConfiguration<GroupAiProviderAccess>
{
    public void Configure(EntityTypeBuilder<GroupAiProviderAccess> builder)
    {
        builder.ToTable("group_ai_provider_access");

        builder.HasKey(x => new { x.GroupId, x.ProviderId });

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
