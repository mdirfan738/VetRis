namespace VetRIS.Domain.Enums;

/// <summary>
/// Mirrors the RIS workflow exactly as specified: Clinic Upload -> Study
/// Created -> In Progress -> Assign Radiologist -> Radiologist Opens Study
/// -> Review Required -> Require Action -> Report Generation -> Study Write
/// Back -> File Distribution -> Final Report -> Study Archived. Invoice /
/// Account Receivable / Payment are tracked separately on Invoice.Status —
/// a study can be archived independently of its billing state.
/// </summary>
public enum StudyStatus
{
    Created = 1,
    InProgress = 2,
    AssignedToRadiologist = 3,
    UnderReview = 4,
    ReviewRequired = 5,
    ActionRequired = 6,
    ReportGenerated = 7,
    WrittenBack = 8,
    Distributed = 9,
    Finalized = 10,
    Archived = 11,
    Cancelled = 12
}
