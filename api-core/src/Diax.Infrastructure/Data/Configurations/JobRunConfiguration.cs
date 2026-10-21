using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("job_runs");
        builder.HasKey(x => new { x.Job, x.RunDate });
        builder.Property(x => x.Job).IsRequired().HasMaxLength(64);
        builder.Property(x => x.RunDate).HasColumnType("date");
        builder.Property(x => x.CompletedAtUtc).IsRequired();
    }
}
