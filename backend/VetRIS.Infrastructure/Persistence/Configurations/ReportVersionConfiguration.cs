using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Reporting;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class ReportVersionConfiguration : IEntityTypeConfiguration<ReportVersion>
{
    public void Configure(EntityTypeBuilder<ReportVersion> builder)
    {
        builder.ToTable("ReportVersions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Findings).IsRequired().HasColumnType("text");
        builder.Property(v => v.Impression).HasColumnType("text");
        builder.Property(v => v.CreatedByUserId).IsRequired().HasMaxLength(100);

        builder.HasIndex(v => v.ReportId);
    }
}
