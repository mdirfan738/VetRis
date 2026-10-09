using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Application.Clinics;

public class ClinicDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public Guid? HospitalId { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }

    public static ClinicDto FromEntity(Clinic clinic) => new()
    {
        Id = clinic.Id,
        Name = clinic.Name,
        Code = clinic.Code,
        HospitalId = clinic.HospitalId,
        ContactPerson = clinic.ContactPerson,
        Phone = clinic.Phone,
        Email = clinic.Email,
        Address = clinic.Address?.ToString(),
        IsActive = clinic.IsActive
    };
}
