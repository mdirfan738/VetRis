using VetRIS.Domain.Entities.Organizations;

namespace VetRIS.Application.Hospitals;

public class HospitalDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }

    public static HospitalDto FromEntity(Hospital hospital) => new()
    {
        Id = hospital.Id,
        Name = hospital.Name,
        Code = hospital.Code,
        Phone = hospital.Phone,
        Email = hospital.Email,
        Address = hospital.Address?.ToString(),
        IsActive = hospital.IsActive
    };
}
