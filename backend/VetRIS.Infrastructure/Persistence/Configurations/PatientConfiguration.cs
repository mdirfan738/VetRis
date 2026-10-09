using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Mrn).IsRequired().HasMaxLength(50);
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Ignore(p => p.FullName); // computed, not a column

        builder.HasIndex(p => new { p.ClinicId, p.Mrn }).IsUnique();

        builder.HasOne<Clinic>().WithMany().HasForeignKey(p => p.ClinicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Hospital>().WithMany().HasForeignKey(p => p.HospitalId).OnDelete(DeleteBehavior.Restrict);
    }
}
