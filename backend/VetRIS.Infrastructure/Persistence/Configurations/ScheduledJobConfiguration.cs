using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Ops;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class ScheduledJobConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        builder.ToTable("Scheduler");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Name).IsRequired().HasMaxLength(150);
        builder.Property(j => j.JobType).IsRequired().HasMaxLength(150);
        builder.Property(j => j.CronExpression).HasMaxLength(100);

        builder.HasIndex(j => j.Name).IsUnique();

        builder.HasMany(j => j.Executions).WithOne().HasForeignKey(e => e.ScheduledJobId).OnDelete(DeleteBehavior.Cascade);
    }
}
