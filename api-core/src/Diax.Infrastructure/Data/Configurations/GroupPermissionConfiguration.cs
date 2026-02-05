using Diax.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
    public void Configure(EntityTypeBuilder<GroupPermission> builder)
    {
        builder.ToTable("group_permissions");

        builder.HasKey(x => new { x.GroupId, x.PermissionId });

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
