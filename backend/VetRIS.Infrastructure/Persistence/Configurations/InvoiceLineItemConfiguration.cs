using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Billing;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description).IsRequired().HasMaxLength(500);

        builder.OwnsOne(li => li.UnitPrice, m =>
        {
            m.Property(x => x.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(li => li.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("LineAmount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("LineAmountCurrency").HasMaxLength(3);
        });

        builder.HasIndex(li => li.InvoiceId);
    }
}
