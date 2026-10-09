using VetRIS.Domain.Common;

namespace VetRIS.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Line1 { get; private set; }
    public string? Line2 { get; private set; }
    public string City { get; private set; }
    public string? State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    private Address()
    {
        Line1 = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    } // EF Core

    public Address(string line1, string city, string postalCode, string country, string? line2 = null, string? state = null)
    {
        if (string.IsNullOrWhiteSpace(line1)) throw new ArgumentException("Address line 1 is required.", nameof(line1));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(country)) throw new ArgumentException("Country is required.", nameof(country));

        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() =>
        string.Join(", ", new[] { Line1, Line2, City, State, PostalCode, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
