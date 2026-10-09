using FluentAssertions;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.ValueObjects;
using Xunit;

namespace VetRIS.Domain.UnitTests.Entities;

public class InvoiceTests
{
    private static Invoice CreateInvoice() =>
        new("INV-001", Guid.NewGuid(), DateTime.UtcNow.AddDays(30));

    [Fact]
    public void Constructor_SetsStatusToDraft()
    {
        CreateInvoice().Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void AddLineItem_UpdatesSubTotalAndTotal()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(5000, "INR"));

        invoice.SubTotal.Amount.Should().Be(5000);
        invoice.TotalAmount.Amount.Should().Be(5000);
    }

    [Fact]
    public void AddLineItem_MultipleItems_SumsCorrectly()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(5000, "INR"));
        invoice.AddLineItem("Contrast", 2, new Money(500, "INR"));

        invoice.SubTotal.Amount.Should().Be(6000); // 5000 + (2 * 500)
    }

    [Fact]
    public void ApplyGst_CalculatesTaxAndTotal()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(1000, "INR"));

        invoice.ApplyGst(0.18m);

        invoice.TaxAmount.Amount.Should().Be(180);
        invoice.TotalAmount.Amount.Should().Be(1180);
    }

    [Fact]
    public void ApplyGst_OutOfRange_Throws()
    {
        var act = () => CreateInvoice().ApplyGst(1.5m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Send_WithoutLineItems_Throws()
    {
        var act = () => CreateInvoice().Send();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Send_RaisesInvoiceGeneratedEvent()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(1000, "INR"));
        invoice.ClearDomainEvents();

        invoice.Send();

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceGeneratedEvent);
        invoice.Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public void RegisterPayment_FullAmount_MarksPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(1000, "INR"));
        invoice.Send();

        invoice.RegisterPayment(new Money(1000, "INR"));

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.Outstanding().Amount.Should().Be(0);
    }

    [Fact]
    public void RegisterPayment_PartialAmount_MarksPartiallyPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(1000, "INR"));
        invoice.Send();

        invoice.RegisterPayment(new Money(400, "INR"));

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.Outstanding().Amount.Should().Be(600);
    }

    [Fact]
    public void RegisterPayment_OnCancelledInvoice_Throws()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(1000, "INR"));
        invoice.Cancel();

        var act = () => invoice.RegisterPayment(new Money(100, "INR"));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddLineItem_AfterSending_Throws()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("CT Scan", 1, new Money(1000, "INR"));
        invoice.Send();

        var act = () => invoice.AddLineItem("Extra", 1, new Money(100, "INR"));
        act.Should().Throw<DomainException>();
    }
}
