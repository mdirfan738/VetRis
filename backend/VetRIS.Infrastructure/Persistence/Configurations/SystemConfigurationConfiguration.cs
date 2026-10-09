using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Admin;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.ToTable("SystemConfigurations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Key).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Value).IsRequired().HasColumnType("text");
        builder.Property(c => c.Category).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Key).IsUnique();
    }
}
