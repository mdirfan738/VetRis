using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Ops;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Level).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Message).IsRequired().HasColumnType("text");
        builder.Property(s => s.Exception).HasColumnType("text");
        builder.Property(s => s.Source).HasMaxLength(200);
        builder.Property(s => s.CorrelationId).HasMaxLength(100);

        builder.HasIndex(s => s.Level);
        builder.HasIndex(s => s.TimestampUtc);
        builder.HasIndex(s => s.CorrelationId);
    }
}
