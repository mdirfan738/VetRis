using FluentAssertions;
using VetRIS.Domain.ValueObjects;
using Xunit;

namespace VetRIS.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        var act = () => new Money(-1, "INR");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LowercaseCurrency_IsUppercased()
    {
        var money = new Money(100, "inr");
        money.Currency.Should().Be("INR");
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        Money.Zero("INR").Amount.Should().Be(0);
    }

    [Fact]
    public void Add_SameCurrency_Sums()
    {
        var result = new Money(100, "INR").Add(new Money(50, "INR"));
        result.Amount.Should().Be(150);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var act = () => new Money(100, "INR").Add(new Money(50, "USD"));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_SameCurrency_Subtracts()
    {
        var result = new Money(100, "INR").Subtract(new Money(30, "INR"));
        result.Amount.Should().Be(70);
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = new Money(100, "INR");
        var b = new Money(100, "INR");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        var a = new Money(100, "INR");
        var b = new Money(100, "USD");

        a.Should().NotBe(b);
    }
}
