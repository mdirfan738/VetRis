using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Application.Radiologists;

public class RadiologistDto
{
    public Guid Id { get; init; }
    public Guid ApplicationUserId { get; init; }
    public string LicenseNumber { get; init; } = default!;
    public string? Specialization { get; init; }
    public int MaxDailyWorkload { get; init; }
    public bool IsActive { get; init; }

    public static RadiologistDto FromEntity(Radiologist radiologist) => new()
    {
        Id = radiologist.Id,
        ApplicationUserId = radiologist.ApplicationUserId,
        LicenseNumber = radiologist.LicenseNumber,
        Specialization = radiologist.Specialization,
        MaxDailyWorkload = radiologist.MaxDailyWorkload,
        IsActive = radiologist.IsActive
    };
}
