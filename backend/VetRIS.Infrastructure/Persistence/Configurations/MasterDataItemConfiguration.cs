using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Admin;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class MasterDataItemConfiguration : IEntityTypeConfiguration<MasterDataItem>
{
    public void Configure(EntityTypeBuilder<MasterDataItem> builder)
    {
        builder.ToTable("MasterData");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Category).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Code).IsRequired().HasMaxLength(100);
        builder.Property(m => m.DisplayValue).IsRequired().HasMaxLength(300);

        builder.HasIndex(m => new { m.Category, m.Code }).IsUnique();
    }
}
