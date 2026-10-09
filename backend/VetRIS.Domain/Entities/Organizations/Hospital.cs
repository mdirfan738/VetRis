using VetRIS.Domain.Common;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Domain.Entities.Organizations;

public class Hospital : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public Address? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Hospital()
    {
    } // EF Core

    public Hospital(string name, string code, Address? address = null, string? phone = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Hospital name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Hospital code is required.", nameof(code));

        Name = name;
        Code = code;
        Address = address;
        Phone = phone;
        Email = email;
    }

    public void UpdateDetails(string name, Address? address, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Hospital name is required.", nameof(name));

        Name = name;
        Address = address;
        Phone = phone;
        Email = email;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
