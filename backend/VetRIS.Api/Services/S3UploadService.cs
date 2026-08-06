using Amazon.S3;
using Amazon.S3.Transfer;

namespace VetRIS.Api.Services;

public interface IS3UploadService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct);
}

// Uploads DICOM files to the "active" bucket. Server-side encryption is
// enforced at the bucket policy level (SSE-KMS, see terraform/modules/storage),
// so no explicit encryption headers are strictly required here, but we set
// them anyway for defense-in-depth / explicitness.
public class S3UploadService : IS3UploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3UploadService> _logger;

    public S3UploadService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3UploadService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["Aws:DicomActiveBucket"]
            ?? throw new InvalidOperationException("Aws:DicomActiveBucket is not configured");
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        var key = $"studies/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{fileName}";

        var transferUtility = new TransferUtility(_s3Client);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = key,
            BucketName = _bucketName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/dicom" : contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
        };

        _logger.LogInformation("Uploading study file to s3://{Bucket}/{Key}", _bucketName, key);
        await transferUtility.UploadAsync(uploadRequest, ct);

        return key;
    }
}
