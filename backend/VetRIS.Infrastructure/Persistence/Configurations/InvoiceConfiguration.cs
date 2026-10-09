using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => new { i.ClinicId, i.Status });

        builder.HasOne<Clinic>().WithMany().HasForeignKey(i => i.ClinicId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(i => i.SubTotal, m =>
        {
            m.Property(x => x.Amount).HasColumnName("SubTotalAmount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("SubTotalCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(i => i.TaxAmount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("TaxAmount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("TaxCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(i => i.TotalAmount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("TotalCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(i => i.AmountPaid, m =>
        {
            m.Property(x => x.Amount).HasColumnName("AmountPaid").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("AmountPaidCurrency").HasMaxLength(3);
        });

        builder.HasMany(i => i.LineItems).WithOne().HasForeignKey(li => li.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.LineItems).AutoInclude();
    }
}
