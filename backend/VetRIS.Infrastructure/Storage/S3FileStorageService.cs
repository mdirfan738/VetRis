using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using VetRIS.Application.Common.Interfaces;

namespace VetRIS.Infrastructure.Storage;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;

    public S3FileStorageService(IAmazonS3 s3Client, IOptions<S3Options> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await _s3Client.GetObjectAsync(_options.BucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
    }

    public async Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        var copyRequest = new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = sourceKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destinationKey,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.CopyObjectAsync(copyRequest, cancellationToken);
        await _s3Client.DeleteObjectAsync(_options.BucketName, sourceKey, cancellationToken);
    }

    public string GetPresignedDownloadUrl(string key, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return _s3Client.GetPreSignedURL(request);
    }
}
