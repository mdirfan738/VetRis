using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> builder)
    {
        builder.ToTable("Hospitals");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Code).IsRequired().HasMaxLength(50);
        builder.Property(h => h.Phone).HasMaxLength(30);
        builder.Property(h => h.Email).HasMaxLength(256);

        builder.HasIndex(h => h.Code).IsUnique();

        builder.OwnsOne(h => h.Address, a =>
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
