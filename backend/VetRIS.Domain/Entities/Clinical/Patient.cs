using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Clinical;

public class Patient : BaseEntity
{
    public string Mrn { get; private set; } = default!; // Medical Record Number, unique per clinic
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid? HospitalId { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private Patient()
    {
    } // EF Core

    public Patient(string mrn, string firstName, string lastName, DateTime dateOfBirth, Gender gender,
        Guid clinicId, Guid? hospitalId = null, string? phone = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(mrn)) throw new ArgumentException("MRN is required.", nameof(mrn));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));

        Mrn = mrn;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        ClinicId = clinicId;
        HospitalId = hospitalId;
        Phone = phone;
        Email = email;
    }

    public void UpdateContactDetails(string? phone, string? email)
    {
        Phone = phone;
        Email = email;
    }
}
