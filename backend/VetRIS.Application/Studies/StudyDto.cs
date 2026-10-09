using VetRIS.Domain.Entities.Clinical;

namespace VetRIS.Application.Studies;

public class StudyDto
{
    public Guid Id { get; init; }
    public string AccessionNumber { get; init; } = default!;
    public string? StudyInstanceUid { get; init; }
    public Guid PatientId { get; init; }
    public Guid ClinicId { get; init; }
    public Guid? HospitalId { get; init; }
    public string Modality { get; init; } = default!;
    public string? BodyPart { get; init; }
    public string Priority { get; init; } = default!;
    public string Status { get; init; } = default!;
    public Guid? AssignedRadiologistId { get; init; }
    public string? ReferringPhysician { get; init; }
    public string? ClinicalHistory { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime? StudyDateUtc { get; init; }
    public bool IsCriticalFinding { get; init; }
    public int SeriesCount { get; init; }

    public static StudyDto FromEntity(Study study) => new()
    {
        Id = study.Id,
        AccessionNumber = study.AccessionNumber,
        StudyInstanceUid = study.StudyInstanceUid,
        PatientId = study.PatientId,
        ClinicId = study.ClinicId,
        HospitalId = study.HospitalId,
        Modality = study.Modality.ToString(),
        BodyPart = study.BodyPart,
        Priority = study.Priority.ToString(),
        Status = study.Status.ToString(),
        AssignedRadiologistId = study.AssignedRadiologistId,
        ReferringPhysician = study.ReferringPhysician,
        ClinicalHistory = study.ClinicalHistory,
        ScheduledAtUtc = study.ScheduledAtUtc,
        StudyDateUtc = study.StudyDateUtc,
        IsCriticalFinding = study.IsCriticalFinding,
        SeriesCount = study.Series.Count
    };
}
