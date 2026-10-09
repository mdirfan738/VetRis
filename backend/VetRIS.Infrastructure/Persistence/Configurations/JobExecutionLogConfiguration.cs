using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Ops;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class JobExecutionLogConfiguration : IEntityTypeConfiguration<JobExecutionLog>
{
    public void Configure(EntityTypeBuilder<JobExecutionLog> builder)
    {
        builder.ToTable("Jobs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ErrorMessage).HasColumnType("text");

        builder.HasIndex(e => e.ScheduledJobId);
        builder.HasIndex(e => e.Status);
    }
}
