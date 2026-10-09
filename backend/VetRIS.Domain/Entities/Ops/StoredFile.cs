using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Ops;

/// <summary>
/// Maps to the spec's "Files" table — a single record type for anything
/// persisted to AWS S3 (or local disk before the archive job runs): DICOM
/// archives, PDF reports, invoices, attachments, logs, backups. Consolidates
/// what would otherwise be a duplicate "Attachment" concept.
/// </summary>
public class StoredFile : BaseEntity
{
    public string FileName { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public FileCategory Category { get; private set; }
    public string ContentType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public string? Checksum { get; private set; }
    public bool IsEncrypted { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string UploadedByUserId { get; private set; } = default!;

    private StoredFile()
    {
    } // EF Core

    public StoredFile(string fileName, string storageKey, FileCategory category, string contentType,
        long fileSizeBytes, string uploadedByUserId, string? relatedEntityType = null, Guid? relatedEntityId = null,
        string? checksum = null, bool isEncrypted = false)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.", nameof(storageKey));

        FileName = fileName;
        StorageKey = storageKey;
        Category = category;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedByUserId = uploadedByUserId;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        Checksum = checksum;
        IsEncrypted = isEncrypted;
    }
}
