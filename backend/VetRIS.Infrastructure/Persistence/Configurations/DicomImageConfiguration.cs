using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class DicomImageConfiguration : IEntityTypeConfiguration<DicomImage>
{
    public void Configure(EntityTypeBuilder<DicomImage> builder)
    {
        builder.ToTable("Images");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SopInstanceUid).IsRequired().HasMaxLength(128);
        builder.Property(i => i.StorageKey).IsRequired().HasMaxLength(512);
        builder.Property(i => i.TransferSyntaxUid).HasMaxLength(128);
        builder.Property(i => i.Checksum).HasMaxLength(128);

        builder.HasIndex(i => i.SopInstanceUid).IsUnique();
        builder.HasIndex(i => i.SeriesId);
    }
}
