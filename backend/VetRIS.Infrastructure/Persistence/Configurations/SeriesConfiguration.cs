using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("Series");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SeriesInstanceUid).IsRequired().HasMaxLength(128);
        builder.Property(s => s.BodyPart).HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => s.SeriesInstanceUid).IsUnique();
        builder.HasIndex(s => s.StudyId);

        builder.HasMany(s => s.Images).WithOne().HasForeignKey(i => i.SeriesId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Images).AutoInclude();
    }
}
