using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Clinical;

/// <summary>Maps to the spec's "Images" table — one row per DICOM SOP Instance.</summary>
public class DicomImage : BaseEntity
{
    public Guid SeriesId { get; private set; }
    public string SopInstanceUid { get; private set; } = default!;
    public int InstanceNumber { get; private set; }
    public string StorageKey { get; private set; } = default!; // S3 key, or local temp path before archiving
    public long FileSizeBytes { get; private set; }
    public string? TransferSyntaxUid { get; private set; }
    public string? Checksum { get; private set; } // for duplicate detection / integrity validation on ingest

    private DicomImage()
    {
    } // EF Core

    public DicomImage(Guid seriesId, string sopInstanceUid, int instanceNumber, string storageKey,
        long fileSizeBytes, string? transferSyntaxUid = null, string? checksum = null)
    {
        if (seriesId == Guid.Empty) throw new ArgumentException("A parent series is required.", nameof(seriesId));
        if (string.IsNullOrWhiteSpace(sopInstanceUid)) throw new ArgumentException("SopInstanceUID is required.", nameof(sopInstanceUid));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.", nameof(storageKey));

        SeriesId = seriesId;
        SopInstanceUid = sopInstanceUid;
        InstanceNumber = instanceNumber;
        StorageKey = storageKey;
        FileSizeBytes = fileSizeBytes;
        TransferSyntaxUid = transferSyntaxUid;
        Checksum = checksum;
    }
}
