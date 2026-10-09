using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class StudyStatusHistoryConfiguration : IEntityTypeConfiguration<StudyStatusHistory>
{
    public void Configure(EntityTypeBuilder<StudyStatusHistory> builder)
    {
        builder.ToTable("StudyStatusHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangedBy).HasMaxLength(100);
        builder.Property(h => h.Notes).HasMaxLength(1000);

        builder.HasIndex(h => h.StudyId);
    }
}
