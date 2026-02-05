using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class GroupAiModelAccessConfiguration : IEntityTypeConfiguration<GroupAiModelAccess>
{
    public void Configure(EntityTypeBuilder<GroupAiModelAccess> builder)
    {
        builder.ToTable("group_ai_model_access");

        builder.HasKey(x => new { x.GroupId, x.AiModelId });

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Model)
            .WithMany()
            .HasForeignKey(x => x.AiModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
