using FluentAssertions;
using VetRIS.Domain.ValueObjects;
using Xunit;

namespace VetRIS.Domain.UnitTests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Constructor_MissingLine1_Throws()
    {
        var act = () => new Address("", "City", "12345", "Country");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_MissingCity_Throws()
    {
        var act = () => new Address("123 Main St", "", "12345", "Country");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Address("123 Main St", "Mumbai", "400001", "India");
        var b = new Address("123 Main St", "Mumbai", "400001", "India");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentCity_AreNotEqual()
    {
        var a = new Address("123 Main St", "Mumbai", "400001", "India");
        var b = new Address("123 Main St", "Delhi", "400001", "India");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_OmitsNullOptionalFields()
    {
        var address = new Address("123 Main St", "Mumbai", "400001", "India");
        address.ToString().Should().Be("123 Main St, Mumbai, 400001, India");
    }
}
