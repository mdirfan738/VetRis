namespace VetRIS.Infrastructure.Storage;

public class S3Options
{
    public const string SectionName = "S3";

    public string BucketName { get; set; } = default!;
    public string Region { get; set; } = "ap-south-1";
    public string ArchivePrefix { get; set; } = "archive/";
}
