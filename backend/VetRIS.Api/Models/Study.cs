namespace VetRIS.Api.Models;

// Minimal Study entity. PatientId is stored as an opaque string here —
// in a real system, treat this and any other identifier as PHI: encrypt
// at rest (RDS storage encryption covers this), restrict access via IAM/DB
// grants, and log all reads for HIPAA audit trail purposes.
public class Study
{
    public int Id { get; set; }

    public string PatientId { get; set; } = string.Empty;

    public DateTime StudyDate { get; set; }

    // Key of the object in the vetris-dicom-active S3 bucket
    public string S3Key { get; set; } = string.Empty;

    // e.g. "Received", "InProgress", "ReviewRequired", "Final"
    public string Status { get; set; } = "Received";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
