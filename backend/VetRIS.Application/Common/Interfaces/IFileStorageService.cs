namespace VetRIS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over object storage (AWS S3 in production). Kept here so
/// Application handlers and workflow-service jobs never reference the AWS
/// SDK directly — Infrastructure supplies the S3 implementation.
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Copies to destinationKey then deletes sourceKey — used by StudyArchiveJob to move DICOM files into cold storage.</summary>
    Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);

    string GetPresignedDownloadUrl(string key, TimeSpan expiry);
}
