using FluentAssertions;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.ValueObjects;
using Xunit;

namespace VetRIS.Domain.UnitTests.Entities;

public class PaymentTests
{
    private static Payment CreatePayment() =>
        new(Guid.NewGuid(), new Money(500, "INR"), PaymentMethod.Card);

    [Fact]
    public void Constructor_SetsStatusToPending()
    {
        CreatePayment().Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Constructor_ZeroAmount_Throws()
    {
        var act = () => new Payment(Guid.NewGuid(), new Money(0, "INR"), PaymentMethod.Cash);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkCompleted_RaisesPaymentReceivedEvent()
    {
        var payment = CreatePayment();

        payment.MarkCompleted();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.PaidAtUtc.Should().NotBeNull();
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentReceivedEvent);
    }

    [Fact]
    public void MarkCompleted_Twice_ThrowsOnSecondCall()
    {
        var payment = CreatePayment();
        payment.MarkCompleted();

        var act = () => payment.MarkCompleted();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Refund_BeforeCompleted_Throws()
    {
        var act = () => CreatePayment().Refund();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Refund_AfterCompleted_Succeeds()
    {
        var payment = CreatePayment();
        payment.MarkCompleted();

        payment.Refund();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Cancel_AfterCompleted_Throws()
    {
        var payment = CreatePayment();
        payment.MarkCompleted();

        var act = () => payment.Cancel();
        act.Should().Throw<DomainException>();
    }
}
