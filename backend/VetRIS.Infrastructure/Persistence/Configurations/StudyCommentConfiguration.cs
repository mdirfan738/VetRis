using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class StudyCommentConfiguration : IEntityTypeConfiguration<StudyComment>
{
    public void Configure(EntityTypeBuilder<StudyComment> builder)
    {
        builder.ToTable("StudyComments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Comment).IsRequired().HasMaxLength(2000);

        builder.HasIndex(c => c.StudyId);
    }
}
