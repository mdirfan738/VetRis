using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Ops;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("Files");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName).IsRequired().HasMaxLength(300);
        builder.Property(f => f.StorageKey).IsRequired().HasMaxLength(512);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(f => f.Checksum).HasMaxLength(128);
        builder.Property(f => f.RelatedEntityType).HasMaxLength(100);
        builder.Property(f => f.UploadedByUserId).IsRequired().HasMaxLength(100);

        builder.HasIndex(f => f.StorageKey).IsUnique();
        builder.HasIndex(f => new { f.RelatedEntityType, f.RelatedEntityId });
        builder.HasIndex(f => f.Category);
    }
}
