using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Infrastructure.Identity;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class RadiologistConfiguration : IEntityTypeConfiguration<Radiologist>
{
    public void Configure(EntityTypeBuilder<Radiologist> builder)
    {
        builder.ToTable("Radiologists");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.LicenseNumber).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Specialization).HasMaxLength(150);
        builder.Property(r => r.SignatureImageStorageKey).HasMaxLength(512);

        builder.HasIndex(r => r.ApplicationUserId).IsUnique();
        builder.HasIndex(r => r.LicenseNumber).IsUnique();

        // No C# navigation on the Domain side (Domain doesn't reference
        // Identity), but the FK constraint itself is still worth having at
        // the database level since Infrastructure knows about both types.
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.ApplicationUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
