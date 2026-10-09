using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Entities.Reporting;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Findings).IsRequired().HasColumnType("text");
        builder.Property(r => r.Impression).HasColumnType("text");
        builder.Property(r => r.Recommendation).HasColumnType("text");
        builder.Property(r => r.SignatureData).HasColumnType("text");
        builder.Property(r => r.PdfStorageKey).HasMaxLength(512);

        builder.HasIndex(r => r.StudyId);
        builder.HasIndex(r => r.RadiologistId);

        builder.HasOne<Study>().WithMany().HasForeignKey(r => r.StudyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Radiologist>().WithMany().HasForeignKey(r => r.RadiologistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReportTemplate>().WithMany().HasForeignKey(r => r.TemplateId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Versions).WithOne().HasForeignKey(v => v.ReportId).OnDelete(DeleteBehavior.Cascade);
    }
}
