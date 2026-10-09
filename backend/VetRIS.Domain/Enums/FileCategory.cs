namespace VetRIS.Domain.Enums;

/// <summary>Drives which S3 prefix/bucket policy a StoredFile lands under.</summary>
public enum FileCategory
{
    DicomStudy = 1,
    Report = 2,
    Invoice = 3,
    Attachment = 4,
    Log = 5,
    Backup = 6
}
