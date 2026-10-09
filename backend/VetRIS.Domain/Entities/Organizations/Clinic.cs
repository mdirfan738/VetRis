using VetRIS.Domain.Common;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Domain.Entities.Organizations;

public class Clinic : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public Guid? HospitalId { get; private set; }
    public Address? Address { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Clinic()
    {
    } // EF Core

    public Clinic(string name, string code, Guid? hospitalId = null, Address? address = null,
        string? contactPerson = null, string? phone = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Clinic name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Clinic code is required.", nameof(code));

        Name = name;
        Code = code;
        HospitalId = hospitalId;
        Address = address;
        ContactPerson = contactPerson;
        Phone = phone;
        Email = email;
    }

    public void UpdateContact(string? contactPerson, string? phone, string? email)
    {
        ContactPerson = contactPerson;
        Phone = phone;
        Email = email;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
