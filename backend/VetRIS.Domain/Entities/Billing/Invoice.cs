using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Domain.Entities.Billing;

public class Invoice : AggregateRoot
{
    public string InvoiceNumber { get; private set; } = default!;
    public Guid ClinicId { get; private set; }
    public Guid? StudyId { get; private set; }
    public Money SubTotal { get; private set; } = default!;
    public Money TaxAmount { get; private set; } = default!;
    public Money TotalAmount { get; private set; } = default!;
    public Money AmountPaid { get; private set; } = default!;
    public InvoiceStatus Status { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }

    private readonly List<InvoiceLineItem> _lineItems = new();
    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    private Invoice()
    {
    } // EF Core

    public Invoice(string invoiceNumber, Guid clinicId, DateTime dueAtUtc, Guid? studyId = null, string currency = "INR")
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) throw new DomainException("Invoice number is required.");

        InvoiceNumber = invoiceNumber;
        ClinicId = clinicId;
        StudyId = studyId;
        DueAtUtc = dueAtUtc;
        IssuedAtUtc = DateTime.UtcNow;
        Status = InvoiceStatus.Draft;
        SubTotal = Money.Zero(currency);
        TaxAmount = Money.Zero(currency);
        TotalAmount = Money.Zero(currency);
        AmountPaid = Money.Zero(currency);
    }

    public void AddLineItem(string description, int quantity, Money unitPrice)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Line items can only be added while the invoice is a draft.");

        _lineItems.Add(new InvoiceLineItem(Id, description, quantity, unitPrice));
        Recalculate(unitPrice.Currency);
    }

    public void ApplyGst(decimal gstRate)
    {
        if (gstRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(gstRate), "GST rate should be expressed as a fraction between 0 and 1.");

        TaxAmount = new Money(Math.Round(SubTotal.Amount * gstRate, 2), SubTotal.Currency);
        TotalAmount = SubTotal.Add(TaxAmount);
    }

    public void Send()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Only a draft invoice can be sent.");
        if (_lineItems.Count == 0)
            throw new DomainException("An invoice needs at least one line item before it can be sent.");

        Status = InvoiceStatus.Sent;
        RaiseDomainEvent(new InvoiceGeneratedEvent(Id, ClinicId, TotalAmount.Amount));
    }

    public void RegisterPayment(Money amount)
    {
        if (Status is InvoiceStatus.Cancelled or InvoiceStatus.Refunded or InvoiceStatus.WrittenOff)
            throw new DomainException($"Cannot register a payment against an invoice with status '{Status}'.");

        AmountPaid = AmountPaid.Add(amount);
        Status = AmountPaid.Amount >= TotalAmount.Amount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }

    public Money Outstanding() => TotalAmount.Subtract(AmountPaid);

    public void MarkOverdue()
    {
        if (Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid)
            Status = InvoiceStatus.Overdue;
    }

    public void Cancel() => Status = InvoiceStatus.Cancelled;
    public void WriteOff() => Status = InvoiceStatus.WrittenOff;

    private void Recalculate(string currency)
    {
        SubTotal = _lineItems.Aggregate(Money.Zero(currency), (acc, li) => acc.Add(li.Amount));
        TotalAmount = SubTotal.Add(TaxAmount);
    }
}
