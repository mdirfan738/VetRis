using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class StudyConfiguration : IEntityTypeConfiguration<Study>
{
    public void Configure(EntityTypeBuilder<Study> builder)
    {
        builder.ToTable("Studies");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.AccessionNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.StudyInstanceUid).HasMaxLength(128);
        builder.Property(s => s.BodyPart).HasMaxLength(100);
        builder.Property(s => s.ReferringPhysician).HasMaxLength(200);
        builder.Property(s => s.ClinicalHistory).HasColumnType("text");
        builder.Property(s => s.Description).HasMaxLength(500);
        // Modality/Priority/Status are stored as their underlying int (EF
        // Core's default for enums) — values are pinned explicitly in the
        // Domain.Enums declarations, so this is stable across reordering.

        builder.HasIndex(s => s.AccessionNumber).IsUnique();
        builder.HasIndex(s => s.StudyInstanceUid);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.ClinicId, s.Status });

        builder.HasOne<Patient>().WithMany().HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(s => s.ClinicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Hospital>().WithMany().HasForeignKey(s => s.HospitalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Radiologist>().WithMany().HasForeignKey(s => s.AssignedRadiologistId).OnDelete(DeleteBehavior.Restrict);

        // True compositions — cascade is safe and correct here, unlike the
        // cross-aggregate references above.
        builder.HasMany(s => s.Series).WithOne().HasForeignKey(se => se.StudyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.StatusHistory).WithOne().HasForeignKey(h => h.StudyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Comments).WithOne().HasForeignKey(c => c.StudyId).OnDelete(DeleteBehavior.Cascade);

        // Application's StudyDto reads study.Series.Count, and a generic
        // IRepository<T>.GetByIdAsync/GetQueryable() has no per-call
        // .Include() mechanism — so without AutoInclude that count would
        // silently come back as 0 on every load.
        builder.Navigation(s => s.Series).AutoInclude();
    }
}
