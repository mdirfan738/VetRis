using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Billing;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TransactionReference).HasMaxLength(200);
        builder.Property(p => p.ReceivedByUserId).HasMaxLength(100);

        builder.HasIndex(p => p.InvoiceId);

        builder.HasOne<Invoice>().WithMany().HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
    }
}
