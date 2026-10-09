using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Ops;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).HasMaxLength(100);
        builder.Property(n => n.RecipientEmail).HasMaxLength(256);
        builder.Property(n => n.RecipientPhone).HasMaxLength(30);
        builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Subject).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Message).IsRequired().HasColumnType("text");
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);

        builder.HasIndex(n => n.RecipientUserId);
        builder.HasIndex(n => n.Status);
    }
}
