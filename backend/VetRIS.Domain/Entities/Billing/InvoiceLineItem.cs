using VetRIS.Domain.Common;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Domain.Entities.Billing;

public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;

    private InvoiceLineItem()
    {
    } // EF Core

    public InvoiceLineItem(Guid invoiceId, string description, int quantity, Money unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Line item description is required.", nameof(description));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Amount = new Money(unitPrice.Amount * quantity, unitPrice.Currency);
    }
}
