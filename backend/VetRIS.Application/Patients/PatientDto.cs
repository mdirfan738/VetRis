using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Application.Patients;

public class PatientDto
{
    public Guid Id { get; init; }
    public string Mrn { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = default!;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public Guid ClinicId { get; init; }
    public Guid? HospitalId { get; init; }

    public static PatientDto FromEntity(Patient patient) => new()
    {
        Id = patient.Id,
        Mrn = patient.Mrn,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        FullName = patient.FullName,
        DateOfBirth = patient.DateOfBirth,
        Gender = patient.Gender.ToString(),
        Phone = patient.Phone,
        Email = patient.Email,
        ClinicId = patient.ClinicId,
        HospitalId = patient.HospitalId
    };
}
