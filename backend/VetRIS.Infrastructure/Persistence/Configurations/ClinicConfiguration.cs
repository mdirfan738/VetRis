using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.ContactPerson).HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(256);

        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasOne<Hospital>().WithMany()
            .HasForeignKey(c => c.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Line1).HasColumnName("AddressLine1").HasMaxLength(200);
            a.Property(x => x.Line2).HasColumnName("AddressLine2").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("AddressCity").HasMaxLength(100);
            a.Property(x => x.State).HasColumnName("AddressState").HasMaxLength(100);
            a.Property(x => x.PostalCode).HasColumnName("AddressPostalCode").HasMaxLength(20);
            a.Property(x => x.Country).HasColumnName("AddressCountry").HasMaxLength(100);
        });
    }
}